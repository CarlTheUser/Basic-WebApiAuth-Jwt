
class WeatherView extends HTMLElement {

    constructor() {
        super();

        const template = document.createElement('template');

        template.innerHTML = `<style>
                                .weathers-container {
                                    display: flex;
                                    justify-content: flex-start;
                                    align-items: stretch;
                                    align-content: flex-start;
                                    gap: 10px;
                                    flex-direction: row;
                                    flex-wrap: wrap;
                                }
                            </style>
                            <button id="loadWeatherButton" type="button">Load Weather</button>
                            <div id="container" class="weathers-container">
                            </div>`;

        const shadowRoot = this.attachShadow({ mode: 'open' });
        shadowRoot.appendChild(template.content.cloneNode(true));

        const loadWeatherButton = shadowRoot.querySelector('#loadWeatherButton');
        const container = shadowRoot.querySelector('#container');

        const loadingView = document.createElement('DIV');
        loadingView.innerHTML = `<span style="font-size:5em;">l o a d i n g . . .</span>`;

        const errorView = document.createElement('DIV');
        errorView.innerHTML = `<span style="font-size:5em; color:red">e r r o r   : (</span>`;

        this.loadWeatherButtonClickedHanler = async function (event) {

            loadWeatherButton.disabled = true;

            empty(container);

            container.appendChild(loadingView);

            const fetchResponse = await fetch('https://localhost:7091/WeatherForecast', {
                method: 'GET',
                headers: {
                    'Content-type': 'application/json; charset=UTF-8',
                    'Accept': 'application/json',
                    'Authorization': `Bearer ${authManager.getToken()}`
                }
            });

            if (fetchResponse.ok) {

                try {
                    const weatherJson = await fetchResponse.json();

                    container.removeChild(loadingView);

                    const fragment = document.createDocumentFragment();

                    let weatherItem = null;

                    weatherJson.forEach(function (item) {

                        weatherItem = document.createElement('weather-item');

                        weatherItem.temperature = `${item.temperatureC} C`;

                        weatherItem.summary = item.summary;

                        fragment.appendChild(weatherItem);
                    });

                    container.appendChild(fragment);

                } catch {

                    empty(container);

                    container.appendChild(errorView);
                }
                

            } else {

                empty(container);

                container.appendChild(errorView);
            }

            loadWeatherButton.disabled = false;

        }.bind(this);

    }

    connectedCallback() {
        const root = this.shadowRoot;

        root.querySelector('#loadWeatherButton').addEventListener('click', this.loadWeatherButtonClickedHanler);
    }

    disconnectedCallback() {
        const root = this.shadowRoot;

        root.querySelector('#loadWeatherButton').removeEventListener('click', this.loadWeatherButtonClickedHanler);
    }

}

customElements.define('weather-view', WeatherView);