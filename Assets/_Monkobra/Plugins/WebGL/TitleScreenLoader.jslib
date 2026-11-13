// tells the WebGL page that the TitleScreen has drawn, called from
// TitleScreen.cs; the FitWindow loading page then fades out over it
mergeInto(LibraryManager.library, {
    MonkobraTitleShown: function () {
        if (typeof window.monkobraTitleShown === "function") {
            window.monkobraTitleShown();
        }
    }
});
