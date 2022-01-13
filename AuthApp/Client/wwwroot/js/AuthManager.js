



var authManager = (function () {

    let currentToken = null;

    //https://stackoverflow.com/questions/38552003/how-to-decode-jwt-token-in-javascript-without-using-a-library
    function parseJwt(token) {
        var base64Url = token.split('.')[1];
        var base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        var jsonPayload = decodeURIComponent(atob(base64).split('').map(function (c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));

        return JSON.parse(jsonPayload);
    };

    https://stackoverflow.com/questions/5968196/how-do-i-check-if-a-cookie-exists
    function getCookie(name) {
        const dc = document.cookie;
        const prefix = name + "=";
        let begin = dc.indexOf("; " + prefix);

        if (begin == -1) {
            begin = dc.indexOf(prefix);
            if (begin != 0) return null;
        }
        else {
            begin += 2;
            let end = document.cookie.indexOf(";", begin);
            if (end == -1) {
                end = dc.length;
            }
        }
        // because unescape has been deprecated, replaced with decodeURI
        //return unescape(dc.substring(begin + prefix.length, end));
        return decodeURI(dc.substring(begin + prefix.length, end));
    }


    return {
        getToken: function () {

            if (currentToken !== null) {

                if (currentToken.expiry > new Date()) {
                    return currentToken.value;
                }

            }

            window.location.href = "../Client/Login.html";
        },

        signIn: async function (username, password, onSuccess, onFailure) {

            const payload = {
                grant_type: 'password',
                username: username,
                password: password
            };

            try {
                const fetchResult = await fetch('https://localhost:7091/api/Auth/Token', {
                    method: 'POST',
                    body: JSON.stringify(payload),
                    headers: {
                        'Content-type': 'application/json; charset=UTF-8',
                        'Accept': 'application/json'
                    }
                });

                if (fetchResult.ok) {

                    const jsonResult = await fetchResult.json();

                    currentToken = {
                        value: jsonResult.access_token,
                        expiry: new Date(jsonResult.access_token_expires)
                    };

                    console.log(jsonResult);
                    console.log(currentToken);

                    onSuccess();

                } else {

                    onFailure({
                        status: fetchResult.status
                    });
                }
            }
            catch{
                onFailure();
            }

            

        },

        getCurrentUserInfo: function () {

            if (currentToken !== null) {
                return parseJwt(currentToken);
            }

            window.location.href = "../Client/Login.html";
        },

        tryRefreshToken: async function () {

            if (getCookie('X-Can-Refresh') !== null) {

                const fetchResult = await fetch('https://localhost:7091/api/Auth/Re', {
                    method: 'POST',
                    headers: {
                        'Accept': 'application/json'
                    }
                });

                if (fetchResult.ok) {

                    const jsonResult = await fetchResult.json();

                    currentToken = {
                        value: jsonResult.access_token,
                        expiry: new Date(jsonResult.access_token_expires)
                    };

                    console.log(jsonResult);
                    console.log(currentToken);

                } else {

                    window.location.href = "../Client/Login.html";
                }

            }
        }
    };
})();

if (!window.location.href.endsWith('/Client/Login.html')) {
    authManager.tryRefreshToken();
} else {
    console.log('Login Page');
}
