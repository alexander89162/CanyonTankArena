mergeInto(LibraryManager.library, {
  MountIDBFS: function() {
    try { FS.mkdir('/idbfs'); } catch(e) {}
    try { FS.mkdir('/idbfs/tank_arena_save_v1'); } catch(e) {}

    FS.mount(IDBFS, {}, '/idbfs/tank_arena_save_v1');

    FS.syncfs(true, function(err) {
      if (err) console.error('IDBFS mount error:', err);
      else console.log('IDBFS ready');
    });
  },
  SyncFiles: function() {
    FS.syncfs(false, function(err) {
      if (err) console.error('IDBFS sync error:', err);
      else console.log('IDBFS synced');
    });
  }
});
