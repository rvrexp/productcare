window.productCareUi = {
    scrollChatToBottom: function (element, smooth) {
        if (!element) {
            return;
        }

        element.scrollTo({
            top: element.scrollHeight,
            behavior: smooth ? "smooth" : "auto"
        });
    },

    focusWithoutScroll: function (element) {
        if (!element) {
            return;
        }

        element.focus({ preventScroll: true });
    }
};