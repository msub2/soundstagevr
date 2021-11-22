mergeInto(LibraryManager.library, {
  CreateBuffer: function() {
    if (window.WEBAudio.bufferSource != null) return;

    const audioCtx = window.WEBAudio.audioContext;
    //const audioCtx = new AudioContext();
    
    window.WEBAudio.bufferSource = audioCtx.createBufferSource();
    window.WEBAudio.bufferSource.loop = true;
    window.WEBAudio.bufferSource.buffer = audioCtx.createBuffer(2, 1024, 44100);

    const gain = audioCtx.createGain();
    gain.gain.value = 0.3;

    window.WEBAudio.bufferSource.connect(gain);
    gain.connect(audioCtx.destination);

    window.WEBAudio.bufferSource.start();
  },
  UpdateBuffer: function(buffer, bufferLength) {
    if (!window.WEBAudio.bufferSource) return;

    for (var i = 0; i < 2; i++) {
      var channel = window.WEBAudio.bufferSource.buffer.getChannelData(i);
      for (var j = 0; j < bufferLength; j++) {
        channel[j] = HEAPF32[(buffer >> 2) + j];
      }
    }
  }
});