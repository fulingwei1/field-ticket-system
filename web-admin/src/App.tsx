import React from 'react';
import { BrowserRouter } from 'react-router-dom';
import AppRoutes from './routes';

const App: React.FC = () => {
  console.log('📱 App component rendering...');
  
  try {
    return (
      <BrowserRouter>
        <AppRoutes />
      </BrowserRouter>
    );
  } catch (error) {
    console.error('❌ App component render error:', error);
    return (
      <div style={{ padding: '20px', fontFamily: 'Arial, sans-serif', color: 'red' }}>
        <h1>App 组件渲染失败</h1>
        <p>错误: {error instanceof Error ? error.message : String(error)}</p>
      </div>
    );
  }
};

export default App;

