class WeatherItem extends HTMLElement {

    static get observedAttributes() { return ['data-temperature', 'data-summary']; }

    constructor() {
        super();

        const template = document.createElement('template');

        template.innerHTML = `<style>
                                    .weather-item {
                                        border: 1px solid #6e6e6e;
                                        padding: 16px;
                                        width: 150px;
                                        border-radius: 3px;
                                    }

                                    .weather-item .temp{
                                        font-size:3em;
                                    }
                            </style>
                             <div class="weather-item">
                                <span id="temparetureText" class="temp"></span>
                                <br/>
                                <span id="summaryText"></span>
                            </div>`;

        const shadowRoot = this.attachShadow({ mode: 'open' });
        shadowRoot.appendChild(template.content.cloneNode(true));
    }

    attributeChangedCallback(name, oldValue, newValue) {

        switch (name) {
            case 'data-temperature':
                this.shadowRoot.querySelector('#temparetureText').innerText = newValue;
                break;
            case 'data-summary':
                this.shadowRoot.querySelector('#summaryText').innerText = newValue;
                break;
        }
    }

    get temperature() {
        return this.getAttribute('data-temperature');
    }

    set temperature(value) {
        this.setAttribute('data-temperature', value);
    }

    get summary() {
        return this.getAttribute('data-summary');
    }

    set summary(value) {
        this.setAttribute('data-summary', value);
    }
}

customElements.define('weather-item', WeatherItem);