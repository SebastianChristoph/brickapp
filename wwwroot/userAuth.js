window.userAuth = {
    hasAuth: function () {
        return !!window.localStorage.getItem('authToken');
    },
    getAuthToken: function () {
        return window.localStorage.getItem('authToken');
    },
    getUsername: function () {
        return window.localStorage.getItem('username');
    },
    setAuthInfo: function (token, username) {
        window.localStorage.setItem('authToken', token);
        window.localStorage.setItem('username', username);
    },
    clearAuth: function () {
        window.localStorage.removeItem('authToken');
        window.localStorage.removeItem('username');
    }
};
