function empty(element) {
    while (element.firstChild) {
        element.removeChild(element.lastChild);
    }
}
