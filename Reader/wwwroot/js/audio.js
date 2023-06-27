var currentAudio = null;

window.playAudio = function (base64Audio) {
    // Stop the current audio if it's playing
    if (currentAudio) {
        currentAudio.pause();
        currentAudio.currentTime = 0;
    }

    // Play the new audio
    currentAudio = new Audio();
    currentAudio.src = 'data:audio/mpeg;base64,' + base64Audio;
    currentAudio.play();
};
