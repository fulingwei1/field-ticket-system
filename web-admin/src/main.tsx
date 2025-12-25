import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App';

const rootElement = document.getElementById('root');
if (!rootElement) {
  throw new Error('Root element not found');
}

console.log('🚀 React app starting...');
console.log('📍 Root element:', rootElement);

try {
  console.log('📦 Loading App component...');
  const root = ReactDOM.createRoot(rootElement);
  
  root.render(
    <React.StrictMode>
      <App />
    </React.StrictMode>
  );
  
  console.log('✅ React app rendered successfully');
} catch (error) {
  console.error('❌ React app render failed:', error);
  console.error('Error stack:', error instanceof Error ? error.stack : 'No stack trace');
  
  rootElement.innerHTML = `
    <div style="padding: 20px; font-family: Arial, sans-serif; color: red;">
      <h1>应用加载失败</h1>
      <p>错误信息: ${error instanceof Error ? error.message : String(error)}</p>
      <pre style="background: #f5f5f5; padding: 10px; overflow: auto;">${error instanceof Error ? error.stack : String(error)}</pre>
      <p>请检查浏览器控制台获取更多信息。</p>
    </div>
  `;
}
