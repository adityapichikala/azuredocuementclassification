import React, { useState, useEffect } from 'react';
import axios from 'axios';
import FileUpload from './FileUpload';
import ChatInterface from './ChatInterface';

function App() {
  const [documents, setDocuments] = useState([]);
  const [selectedDocuments, setSelectedDocuments] = useState([]);

  const fetchDocuments = async () => {
    try {
      const response = await axios.get('http://localhost:7071/api/GetDocuments');
      setDocuments(response.data);
    } catch (error) {
      console.error("Error fetching documents:", error);
    }
  };

  useEffect(() => {
    fetchDocuments();
  }, []);

  const handleUploadSuccess = (filename) => {
    console.log(`File uploaded: ${filename}`);
    fetchDocuments(); // Refresh list after upload
  };

  const toggleDocumentSelection = (fileName) => {
    setSelectedDocuments(prev =>
      prev.includes(fileName)
        ? prev.filter(f => f !== fileName)
        : [...prev, fileName]
    );
  };

  return (
    <div className="app-container">
      <header className="app-header">
        <h1>Document Assistant</h1>
        <p>Upload documents and chat with them instantly.</p>
      </header>

      <main className="app-main">
        <div className="left-panel">
          <section className="upload-section">
            <h2>1. Upload Document</h2>
            <FileUpload onUploadSuccess={handleUploadSuccess} />
          </section>

          <section className="documents-section">
            <h2>2. Select Context</h2>
            <div className="document-list">
              {documents.length === 0 ? (
                <p className="no-docs">No documents found.</p>
              ) : (
                documents.map(doc => (
                  <div key={doc.documentId} className="document-item">
                    <label>
                      <input
                        type="checkbox"
                        checked={selectedDocuments.includes(doc.fileName)}
                        onChange={() => toggleDocumentSelection(doc.fileName)}
                      />
                      <span className="doc-name">{doc.fileName}</span>
                    </label>
                  </div>
                ))
              )}
            </div>
          </section>
        </div>

        <section className="chat-section">
          <h2>3. Chat</h2>
          <ChatInterface selectedDocuments={selectedDocuments} />
        </section>
      </main>

      <style>{`
        .app-container {
          max-width: 1200px;
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
          display: grid;
          grid-template-columns: 350px 1fr;
          gap: 2rem;
          align-items: start;
        }
        .left-panel {
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
        .document-list {
          background: var(--surface-color);
          border: 1px solid var(--border-color);
          border-radius: 8px;
          padding: 1rem;
          max-height: 400px;
          overflow-y: auto;
        }
        .document-item {
          margin-bottom: 0.5rem;
        }
        .document-item label {
          display: flex;
          align-items: center;
          cursor: pointer;
          padding: 0.5rem;
          border-radius: 4px;
          transition: background 0.2s;
        }
        .document-item label:hover {
          background: #f3f2f1;
        }
        .document-item input {
          margin-right: 0.75rem;
        }
        .doc-name {
          font-size: 0.9rem;
          word-break: break-all;
        }
        .no-docs {
          color: var(--text-secondary);
          font-style: italic;
          text-align: center;
        }
        @media (max-width: 768px) {
          .app-main {
            grid-template-columns: 1fr;
          }
        }
      `}</style>
    </div>
  );
}

export default App;
