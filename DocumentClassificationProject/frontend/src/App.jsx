import React from 'react';
import FileUpload from './FileUpload';
import ChatInterface from './ChatInterface';

function App() {
  const handleUploadSuccess = (filename) => {
    console.log(`File uploaded: ${filename}`);
    // Optionally trigger a refresh or notification in chat
  };

  return (
    <div className="app-container">
      <header className="app-header">
        <h1>Document Assistant</h1>
        <p>Upload documents and chat with them instantly.</p>
      </header>

      <main className="app-main">
        <section className="upload-section">
          <h2>1. Upload Document</h2>
          <FileUpload onUploadSuccess={handleUploadSuccess} />
        </section>

        <section className="chat-section">
          <h2>2. Chat</h2>
          <ChatInterface />
        </section>
      </main>

      <style>{`
        .app-container {
          max-width: 800px;
          margin: 0 auto;
          padding: 2rem;
          min-height: 100vh;
        }
        .app-header {
          text-align: center;
          margin-bottom: 3rem;
        }
        .app-header h1 {
          color: var(--primary-color);
          font-size: 2.5rem;
          margin-bottom: 0.5rem;
        }
        .app-header p {
          color: var(--text-secondary);
          font-size: 1.2rem;
        }
        .app-main {
          display: flex;
          flex-direction: column;
          gap: 2rem;
        }
        h2 {
          font-size: 1.2rem;
          color: var(--text-primary);
          margin-bottom: 1rem;
          display: flex;
          align-items: center;
        }
        h2::before {
          content: '';
          display: inline-block;
          width: 4px;
          height: 1.2rem;
          background-color: var(--primary-color);
          margin-right: 0.5rem;
          border-radius: 2px;
        }
      `}</style>
    </div>
  );
}

export default App;
