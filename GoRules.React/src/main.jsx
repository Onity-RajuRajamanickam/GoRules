import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App';
import './styles.css';

async function bootstrap() {
  const response = await fetch('/appsettings.json');
  const appSettings = await response.json();

  ReactDOM.createRoot(document.getElementById('root')).render(
    <React.StrictMode>
      <App apiUrl={appSettings.apiUrl} />
    </React.StrictMode>,
  );
}

bootstrap();
