window.createBlobUrl = function (data, mimeType) {
    var blob = new Blob([data], { type: mimeType });
    var url = URL.createObjectURL(blob);
    return url;
}

window.playAudio = function (audioUrl) {
    var audio = new Audio(audioUrl);
    audio.play();
}
