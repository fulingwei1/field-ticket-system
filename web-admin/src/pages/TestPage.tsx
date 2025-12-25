import React from 'react';

const TestPage: React.FC = () => {
  return (
    <div style={{ padding: '20px', fontFamily: 'Arial, sans-serif' }}>
      <h1>✅ React 应用正常工作！</h1>
      <p>如果您看到这个页面，说明 React 已经成功渲染。</p>
      <p>当前时间: {new Date().toLocaleString()}</p>
    </div>
  );
};

export default TestPage;



