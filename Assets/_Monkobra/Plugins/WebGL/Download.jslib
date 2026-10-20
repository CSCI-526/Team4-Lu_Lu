// browser download of a text file, called from WebDownload.cs
mergeInto(LibraryManager.library, {
    MonkobraDownloadText: function (filenamePtr, contentPtr) {
        var filename = UTF8ToString(filenamePtr);
        var content = UTF8ToString(contentPtr);
        var blob = new Blob([content], {type: "text/markdown;charset=utf-8"});
        var url = URL.createObjectURL(blob);
        var anchor = document.createElement("a");
        anchor.href = url;
        anchor.download = filename;
        anchor.style.display = "none";
        document.body.appendChild(anchor);
        anchor.click();
        document.body.removeChild(anchor);
        // give the browser time to start the download before the url dies
        setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
    }
});
