window.copyToClipboard = function (text) {
    navigator.clipboard.writeText(text).then(function() {
        console.log('Text successfully copied to clipboard!');
    }).catch(function(err) {
        console.error('Unable to copy text: ', err);
    });
}
