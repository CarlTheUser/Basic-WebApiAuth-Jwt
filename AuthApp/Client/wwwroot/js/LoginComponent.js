
class LoginComponent extends HTMLElement {

    constructor() {
        super();

        const template = document.createElement('template');

        template.innerHTML = `<style>
                                .login-card {
                                    background-color: yellow;
                                    border: 1px solid #6e6e6e;
                                    min-width: 250px;
                                    width: auto;
                                    max-width: 550px;
                                    padding: 1em;
                                    border-radius: 3px;
                                    box-shadow: 0px 3px 6px 0px rgba(0,0,0,0.25);
                                }
                            </style>
                             <div class="login-card">
                                <form id="formOverFunction">
                                    <h1>Login</h1>
                                    <div>
                                        <label for="username">Username</label>
                                        <input id="usernameInput" name="username" type="text" placeholder="Username" />
                                    </div>

                                    <div>
                                        <label for="password">Password</label>
                                        <input id="passwordInput" name="password" type="password" placeholder="Password" />
                                    </div>
                                    <span id="serverFeedback"></span>
                                    <button id="loginButton" style="display:block;width:100%;">Login</button>
                                </form>
                            </div>`;

        const shadowRoot = this.attachShadow({ mode: 'open' });
        shadowRoot.appendChild(template.content.cloneNode(true));

        const usernameInput = shadowRoot.querySelector('#usernameInput');
        const passwordInput = shadowRoot.querySelector('#passwordInput');
        const serverFeedback = shadowRoot.querySelector('#serverFeedback');
        const loginButton = shadowRoot.querySelector('#loginButton');

        this._onSuccess = function () {

        }

        function onFailure() {
            serverFeedback.innerText = `Oopsie Whoopsie 🤷👀🤦‍♂️❌‼¯\\_(ツ)_/¯`;
        }

        this.functionOverForm = async function (event) {
            event.preventDefault();

            usernameInput.disabled = passwordInput.disabled = loginButton.disabled = true;

            await authManager.signIn(
                usernameInput.value,
                passwordInput.value,
                this._onSuccess,
                onFailure);

            usernameInput.disabled = passwordInput.disabled = loginButton.disabled = false;

            return false;
        }.bind(this);

    }

    connectedCallback() {
        const root = this.shadowRoot;

        root.querySelector('#formOverFunction').addEventListener('submit', this.functionOverForm);
    }

    disconnectedCallback() {
        const root = this.shadowRoot;

        root.querySelector('#formOverFunction').removeEventListener('submit', this.functionOverForm);
    }

    set onSuccess(handler) {
        this._onSuccess = handler;
    }

    get onSuccess() {
        return this._onSuccess;
    }
}

customElements.define('login-component', LoginComponent);