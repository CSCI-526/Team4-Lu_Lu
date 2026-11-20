import assert from 'node:assert/strict';
import { test } from 'node:test';

// Requires the local demo project; these tests never contact production.
const host = process.env.FIRESTORE_EMULATOR_HOST;
const authHost = process.env.FIREBASE_AUTH_EMULATOR_HOST;
assert.ok(host && authHost, 'Run through firebase emulators:exec');
const root = 'projects/demo-monkobra/databases/(default)/documents';
const api = `http://${host}/v1/${root}`;
const authApi = `http://${authHost}/identitytoolkit.googleapis.com/v1`;

async function request(url, body, token, method = 'POST') {
  const headers = { 'Content-Type': 'application/json' };
  if (token) headers.Authorization = `Bearer ${token}`;
  return fetch(url, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });
}

async function player() {
  const response = await request(`${authApi}/accounts:signUp?key=demo-api-key`,
    { returnSecureToken: true });
  assert.equal(response.status, 200);
  return response.json();
}

function commit(uid, score, name = 'Climber', board = 'normal') {
  return { writes: [{
    update: {
      name: `${root}/leaderboards/${board}/scores/${uid}`,
      fields: { displayName: { stringValue: name } },
    },
    updateMask: { fieldPaths: ['displayName'] },
    updateTransforms: [{
      fieldPath: 'score', maximum: { integerValue: String(score) },
    }],
  }] };
}

function query(limit = 10) {
  return { structuredQuery: {
    from: [{ collectionId: 'scores' }],
    orderBy: [{ field: { fieldPath: 'score' }, direction: 'DESCENDING' }],
    ...(limit === null ? {} : { limit }),
  } };
}

test('leaderboard REST contract, maximum scores and access rules', async () => {
  await fetch(`http://${host}/emulator/v1/projects/demo-monkobra/databases/(default)/documents`,
    { method: 'DELETE' });
  const first = await player();
  const second = await player();
  for (const [user, score] of [[first, 500], [second, 900]]) {
    const response = await request(`${api}:commit`,
      commit(user.localId, score), user.idToken);
    assert.equal(response.status, 200, await response.text());
  }
  // A later low score must never overwrite a high score.
  const low = await request(`${api}:commit`,
    commit(first.localId, 5), first.idToken);
  assert.equal(low.status, 200, await low.text());
  const own = await request(
    `${api}/leaderboards/normal/scores/${first.localId}`,
    undefined, first.idToken, 'GET');
  assert.equal((await own.json()).fields.score.integerValue, '500');
  const top = await request(`${api}/leaderboards/normal:runQuery`,
    query(), first.idToken);
  assert.equal(top.status, 200);
  assert.deepEqual((await top.json()).filter(r => r.document)
    .map(r => r.document.fields.score.integerValue), ['900', '500']);
  const otherBoard = await request(`${api}:commit`,
    commit(first.localId, 42, 'Climber', 'endless'), first.idToken);
  assert.equal(otherBoard.status, 200, await otherBoard.text());
  const endless = await request(`${api}/leaderboards/endless:runQuery`,
    query(), first.idToken);
  assert.deepEqual((await endless.json()).filter(r => r.document)
    .map(r => r.document.fields.score.integerValue), ['42']);
  // Rename updates the same UID's document without replacing a personal best.
  const renamed = await request(`${api}:commit`,
    commit(first.localId, 1, 'Renamed Monkey'), first.idToken);
  assert.equal(renamed.status, 200, await renamed.text());
  const afterRename = await request(`${api}/leaderboards/normal:runQuery`,
    query(), first.idToken);
  const entries = (await afterRename.json()).filter(r => r.document);
  assert.equal(entries.length, 2);
  const renamedEntry = entries.find(r => r.document.name.endsWith(first.localId));
  assert.equal(renamedEntry.document.fields.displayName.stringValue, 'Renamed Monkey');
  assert.equal(renamedEntry.document.fields.score.integerValue, '500');
  const forbidden = [
    ['anonymous write', `${api}:commit`, commit(first.localId, 999)],
    ['write other player', `${api}:commit`, commit(second.localId, 999), first.idToken],
    ['negative score', `${api}:commit`, commit('unused', -1), first.idToken],
    ['long name', `${api}:commit`, commit(first.localId, 999, 'x'.repeat(25)), first.idToken],
    ['unknown board', `${api}:commit`, commit(first.localId, 999, 'Climber', 'cheat'), first.idToken],
    ['unbounded query', `${api}/leaderboards/normal:runQuery`, query(null), first.idToken],
    ['oversized query', `${api}/leaderboards/normal:runQuery`, query(11), first.idToken],
    ['anonymous query', `${api}/leaderboards/normal:runQuery`, query()],
  ];
  // Use the actual owner to isolate score validation from ownership checks.
  const third = await player();
  forbidden.push(['negative own score', `${api}:commit`,
    commit(third.localId, -1), third.idToken]);
  for (const [label, url, body, token] of forbidden) {
    const response = await request(url, body, token);
    assert.equal(response.status, 403, `${label}: ${await response.text()}`);
  }
  const deletion = await request(
    `${api}/leaderboards/normal/scores/${first.localId}`,
    undefined, first.idToken, 'DELETE');
  assert.equal(deletion.status, 403);
  const lower = { writes: [{
    update: {
      name: `${root}/leaderboards/normal/scores/${first.localId}`,
      fields: { displayName: { stringValue: 'Climber' },
        score: { integerValue: '1' } },
    },
  }] };
  assert.equal((await request(`${api}:commit`, lower, first.idToken)).status, 403);
  const extra = commit(first.localId, 999);
  extra.writes[0].update.fields.admin = { booleanValue: true };
  extra.writes[0].updateMask.fieldPaths.push('admin');
  assert.equal((await request(`${api}:commit`, extra, first.idToken)).status, 403);
});
