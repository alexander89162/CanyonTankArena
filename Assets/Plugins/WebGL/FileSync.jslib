mergeInto(LibraryManager.library, {
    JS_FileSystem_Sync: function() {
        if (typeof FS !== 'undefined' && FS.syncfs) {
            FS.syncfs(false, function(err) {
                if (err) console.error("FS.syncfs error: " + err);
            });
        }
    }
});