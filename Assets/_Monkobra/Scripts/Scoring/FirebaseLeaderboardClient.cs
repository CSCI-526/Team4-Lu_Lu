using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Anonymous Auth + Firestore REST, usable in WebGL without a native SDK.
/// Saves a pending personal best before networking; retries on the next loss.
/// </summary>
public sealed class FirebaseLeaderboardClient {
    // Public Members  #########################################################
    public sealed class Result {
        public SessionLeaderboard.Entry[] Entries;
        public long PersonalBest;
    }

    // Public Methods  #########################################################
    public FirebaseLeaderboardClient(FirebaseLeaderboardConfig config) {
        string projectId = config.ProjectId;
        apiKey = config.WebApiKey;
        timeoutS = config.RequestTimeoutS;
        preferencePrefix = "Monkobra.Firebase."
            + (config.UseEmulators ? "Emulator." : "") + projectId + ".";
        documentRoot = "projects/" + projectId
            + "/databases/(default)/documents";
        firestore = config.UseEmulators
            ? "http://127.0.0.1:8080/v1/" : FIRESTORE;
        auth = config.UseEmulators
            ? "http://127.0.0.1:9099/identitytoolkit.googleapis.com/v1/"
            : "https://identitytoolkit.googleapis.com/v1/";
        token = config.UseEmulators
            ? "http://127.0.0.1:9099/securetoken.googleapis.com/v1/"
            : "https://securetoken.googleapis.com/v1/";
    }

    public long RememberScore(string board, long score) {
        string key = preferencePrefix + board + ".best";
        long.TryParse(PlayerPrefs.GetString(key, "0"),
            NumberStyles.Integer, CultureInfo.InvariantCulture, out long old);
        long best = Math.Max(old, Math.Max(0L, score));
        PlayerPrefs.SetString(key, best.ToString(CultureInfo.InvariantCulture));
        PlayerPrefs.Save();
        return best;
    }

    public string PlayerName => PlayerPrefs.GetString(preferencePrefix + "name");

    public static bool TryNormalizeName(string name, out string normalized) {
        normalized = (name ?? "").Trim();
        // Matches the shipped font's Latin glyphs and prevents markup/control
        // characters. Display names are labels, never authentication keys.
        return Regex.IsMatch(normalized, "^[A-Za-z0-9][A-Za-z0-9 _-]{0,15}$");
    }

    public bool RememberName(string name) {
        if (!TryNormalizeName(name, out string normalized)) {
            return false;
        }
        PlayerPrefs.SetString(preferencePrefix + "name", normalized);
        PlayerPrefs.Save();
        return true;
    }

    public async Task<Result> SubmitAndLoad(string board, long score, int limit) {
        if (board != "normal" && board != "endless") {
            throw new ArgumentException("Unknown leaderboard.", nameof(board));
        }
        limit = Math.Max(1, Math.Min(10, limit));
        RememberScore(board, score);
        // One identity refresh/write at a time, including across scene loads.
        await requests.WaitAsync();
        try {
            await SignIn();
            long best = RememberScore(board, score);
            string parent = documentRoot + "/leaderboards/" + board;
            string playerDocument = parent + "/scores/" + userId;
            string displayName = PlayerName;
            if (!TryNormalizeName(displayName, out displayName)) {
                displayName = "Climber " + userId.Substring(
                    0, Math.Min(6, userId.Length)
                );
            }
            NameDocument document = new NameDocument {
                name = playerDocument,
                fields = new NameFields {
                    displayName = new StringValue { stringValue = displayName }
                }
            };
            // The server's maximum transform keeps a higher score even when
            // two clients submit out of order. The mask preserves that score.
            string commit = "{\"writes\":[{\"update\":"
                + JsonUtility.ToJson(document)
                + ",\"updateMask\":{\"fieldPaths\":[\"displayName\"]},"
                + "\"updateTransforms\":[{\"fieldPath\":\"score\","
                + "\"maximum\":{\"integerValue\":\""
                + best.ToString(CultureInfo.InvariantCulture) + "\"}}]}]}";
            await Send(firestore + documentRoot + ":commit", commit, true);
            string query = "{\"structuredQuery\":{\"from\":[{"
                + "\"collectionId\":\"scores\"}],\"orderBy\":[{\"field\":{"
                + "\"fieldPath\":\"score\"},\"direction\":\"DESCENDING\"}],"
                + "\"limit\":" + limit + "}}";
            string response = await Send(
                firestore + parent + ":runQuery", query, true
            );
            QueryResults parsed = JsonUtility.FromJson<QueryResults>(
                "{\"items\":" + response + "}"
            );
            List<SessionLeaderboard.Entry> entries =
                new List<SessionLeaderboard.Entry>();
            bool foundPlayer = false;
            foreach (QueryResult row in parsed.items) {
                Document item = row.document;
                if (item == null || string.IsNullOrEmpty(item.name)) {
                    continue; // Firestore's empty-result readTime envelope.
                }
                long value = ReadScore(item);
                bool isPlayer = item.name == playerDocument;
                if (isPlayer) {
                    best = Math.Max(best, value);
                    foundPlayer = true;
                }
                entries.Add(new SessionLeaderboard.Entry(
                    item.fields.displayName.stringValue, value, isPlayer,
                    entries.Count
                ));
            }
            if (!foundPlayer) {
                string ownJson = await Send(
                    firestore + playerDocument, null, true
                );
                best = Math.Max(best,
                    ReadScore(JsonUtility.FromJson<Document>(ownJson)));
            }
            RememberScore(board, best);
            return new Result { Entries = entries.ToArray(), PersonalBest = best };
        }
        finally {
            requests.Release();
        }
    }

    // private members  ########################################################
    private const string FIRESTORE = "https://firestore.googleapis.com/v1/";
    private static readonly SemaphoreSlim requests = new SemaphoreSlim(1, 1);
    private readonly string firestore;
    private readonly string auth;
    private readonly string token;
    private readonly string apiKey;
    private readonly int timeoutS;
    private readonly string preferencePrefix;
    private readonly string documentRoot;
    private string idToken;
    private string userId;

    // private methods  ########################################################
    private async Task SignIn() {
        string refresh = PlayerPrefs.GetString(preferencePrefix + "refresh");
        string json;
        if (string.IsNullOrEmpty(refresh)) {
            json = await Send(
                auth + "accounts:signUp?key="
                    + UnityWebRequest.EscapeURL(apiKey),
                "{\"returnSecureToken\":true}", false
            );
            Identity identity = JsonUtility.FromJson<Identity>(json);
            idToken = identity.idToken;
            userId = identity.localId;
            refresh = identity.refreshToken;
        }
        else {
            json = await Send(
                token + "token?key="
                    + UnityWebRequest.EscapeURL(apiKey),
                "grant_type=refresh_token&refresh_token="
                    + UnityWebRequest.EscapeURL(refresh), false,
                "application/x-www-form-urlencoded"
            );
            Identity identity = JsonUtility.FromJson<Identity>(json);
            idToken = identity.id_token;
            userId = identity.user_id;
            refresh = identity.refresh_token;
        }
        if (string.IsNullOrEmpty(idToken) || string.IsNullOrEmpty(userId)
            || string.IsNullOrEmpty(refresh)) {
            throw new InvalidOperationException("Firebase sign-in failed.");
        }
        PlayerPrefs.SetString(preferencePrefix + "refresh", refresh);
        PlayerPrefs.Save();
    }

    private async Task<string> Send(
        string url, string body, bool authenticated,
        string contentType = "application/json"
    ) {
        using (UnityWebRequest request = new UnityWebRequest(
            url, body == null ? "GET" : "POST"
        )) {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = timeoutS;
            if (body != null) {
                request.uploadHandler = new UploadHandlerRaw(
                    Encoding.UTF8.GetBytes(body)
                );
                request.SetRequestHeader("Content-Type", contentType);
            }
            if (authenticated) {
                request.SetRequestHeader("Authorization", "Bearer " + idToken);
            }
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            TaskCompletionSource<bool> completion =
                new TaskCompletionSource<bool>();
            operation.completed += _ => completion.TrySetResult(true);
            if (operation.isDone) {
                completion.TrySetResult(true);
            }
            await completion.Task;
            if (request.result != UnityWebRequest.Result.Success) {
                // Never log the URL (API key), token, or response credentials.
                string reason = request.responseCode == 403
                    ? "access denied; check Firestore rules and API settings"
                    : request.responseCode == 429
                        ? "quota reached; try again later"
                        : "connection or Firebase configuration error";
                throw new InvalidOperationException(
                    "Firebase HTTP " + request.responseCode + ": " + reason
                );
            }
            return request.downloadHandler.text;
        }
    }

    private static long ReadScore(Document document) {
        if (document?.fields?.score == null || !long.TryParse(
            document.fields.score.integerValue, NumberStyles.Integer,
            CultureInfo.InvariantCulture, out long value
        ) || value < 0) {
            throw new InvalidOperationException("Invalid leaderboard score.");
        }
        return value;
    }

    // REST wire types: public fields are required by Unity's JSON serializer.
    // Preserve them under IL2CPP even though only FromJson writes most fields.
    [Serializable]
    private sealed class NameDocument {
        public string name;
        public NameFields fields;
    }

    [Serializable]
    private sealed class NameFields {
        public StringValue displayName;
    }

    [Serializable, UnityEngine.Scripting.Preserve]
    private sealed class Identity {
        public string idToken;
        public string localId;
        public string refreshToken;
        public string id_token;
        public string user_id;
        public string refresh_token;
    }

    [Serializable, UnityEngine.Scripting.Preserve]
    private sealed class QueryResults {
        public QueryResult[] items;
    }

    [Serializable, UnityEngine.Scripting.Preserve]
    private sealed class QueryResult {
        public Document document;
    }

    [Serializable, UnityEngine.Scripting.Preserve]
    private sealed class Document {
        public string name;
        public Fields fields;
    }

    [Serializable, UnityEngine.Scripting.Preserve]
    private sealed class Fields {
        public StringValue displayName;
        public IntegerValue score;
    }

    [Serializable, UnityEngine.Scripting.Preserve]
    private sealed class StringValue {
        public string stringValue;
    }

    [Serializable, UnityEngine.Scripting.Preserve]
    private sealed class IntegerValue {
        public string integerValue;
    }
}
