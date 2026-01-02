import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import './index.css'
import App from './App.tsx'
import Auth0ProviderWithHistory from './auth/Auth0ProviderWithHistory'
// Add global error handler for Auth0 errors
window.addEventListener('error', (event) => {
  console.error('Global error caught:', event.error);
});

window.addEventListener('unhandledrejection', (event) => {
  console.error('Unhandled promise rejection:', event.reason);
});

// Debug: Check if we're in a callback
console.log('Current URL:', window.location.href);
console.log('Has code param:', new URLSearchParams(window.location.search).has('code'));
console.log('Has state param:', new URLSearchParams(window.location.search).has('state'));

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <Auth0ProviderWithHistory>
        <App />
      </Auth0ProviderWithHistory>
    </BrowserRouter>
  </StrictMode>,
)
