window.playAudio = function (base64Audio) {
    var audio = new Audio();
    audio.src = 'data:audio/mpeg;base64,' + base64Audio;
    audio.play();
};
