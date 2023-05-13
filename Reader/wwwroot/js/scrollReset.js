window.scrollToTopOfElement = (elementId) => {
    var element = document.getElementById(elementId);
    if (element) {
        element.scrollTop = 0;
    }
}
