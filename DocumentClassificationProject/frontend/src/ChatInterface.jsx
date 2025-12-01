import React, { useState, useRef, useEffect } from 'react';
import axios from 'axios';

const ChatInterface = () => {
  const [messages, setMessages] = useState([
    { id: 1, text: "Hello! Upload a document and ask me anything about it.", sender: 'bot' }
  ]);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const messagesEndRef = useRef(null);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  const handleSend = async (e) => {
    e.preventDefault();
    if (!input.trim()) return;

    const userMessage = { id: Date.now(), text: input, sender: 'user' };
    setMessages(prev => [...prev, userMessage]);
    setInput('');
    setLoading(true);

    try {
      const response = await axios.post('http://localhost:7071/api/Chat', {
        query: userMessage.text
      });

      let responseText = "I couldn't find an answer.";
      if (response.data) {
        if (typeof response.data === 'string') {
          responseText = response.data;
        } else if (response.data.answer) {
          responseText = response.data.answer;
        }
      }

      const botMessage = {
        id: Date.now() + 1,
        text: responseText,
        sender: 'bot'
      };
      setMessages(prev => [...prev, botMessage]);
    } catch (error) {
      console.error("Chat error:", error);
      let errorText = "Sorry, I encountered an error processing your request.";

      if (error.response && error.response.data) {
        // Try to get the error message from the backend response
        // The backend returns a string in some cases or JSON
        if (typeof error.response.data === 'string') {
          errorText = error.response.data;
        } else if (error.response.data.message) {
          errorText = error.response.data.message;
        }
      }

      const errorMessage = {
        id: Date.now() + 1,
        text: errorText,
        sender: 'bot',
        isError: true
      };
      setMessages(prev => [...prev, errorMessage]);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="chat-container">
      <div className="messages-area">
        {messages.map((msg) => (
          <div key={msg.id} className={`message ${msg.sender} ${msg.isError ? 'error' : ''}`}>
            <div className="message-content">{msg.text}</div>
          </div>
        ))}
        {loading && (
          <div className="message bot loading">
            <div className="typing-indicator">
              <span></span><span></span><span></span>
            </div>
          </div>
        )}
        <div ref={messagesEndRef} />
      </div>
      <form onSubmit={handleSend} className="input-area">
        <input
          type="text"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder="Type your question..."
          disabled={loading}
        />
        <button type="submit" disabled={loading || !input.trim()}>Send</button>
      </form>

      <style>{`
        .chat-container {
          display: flex;
          flex-direction: column;
          height: 500px;
          border: 1px solid var(--border-color);
          border-radius: 8px;
          background-color: var(--surface-color);
          overflow: hidden;
          box-shadow: var(--shadow-sm);
        }
        .messages-area {
          flex: 1;
          padding: 1rem;
          overflow-y: auto;
          display: flex;
          flex-direction: column;
          gap: 0.5rem;
        }
        .message {
          max-width: 80%;
          padding: 0.75rem 1rem;
          border-radius: 1rem;
          line-height: 1.4;
        }
        .message.user {
          align-self: flex-end;
          background-color: var(--primary-color);
          color: white;
          border-bottom-right-radius: 0.25rem;
        }
        .message.bot {
          align-self: flex-start;
          background-color: #f3f2f1;
          color: var(--text-primary);
          border-bottom-left-radius: 0.25rem;
        }
        .message.error {
          background-color: #fde7e9;
          color: var(--error-color);
        }
        .input-area {
          display: flex;
          padding: 1rem;
          border-top: 1px solid var(--border-color);
          background-color: var(--surface-color);
        }
        .input-area input {
          flex: 1;
          padding: 0.75rem;
          border: 1px solid var(--border-color);
          border-radius: 4px;
          margin-right: 0.5rem;
          font-size: 1rem;
          outline: none;
        }
        .input-area input:focus {
          border-color: var(--primary-color);
        }
        .input-area button {
          padding: 0 1.5rem;
          background-color: var(--primary-color);
          color: white;
          border: none;
          border-radius: 4px;
          font-weight: 600;
          transition: background-color 0.2s;
        }
        .input-area button:hover:not(:disabled) {
          background-color: var(--primary-hover);
        }
        .input-area button:disabled {
          background-color: #c8c6c4;
          cursor: not-allowed;
        }
        .typing-indicator {
          display: flex;
          gap: 4px;
        }
        .typing-indicator span {
          width: 8px;
          height: 8px;
          background-color: #8a8886;
          border-radius: 50%;
          animation: bounce 1.4s infinite ease-in-out both;
        }
        .typing-indicator span:nth-child(1) { animation-delay: -0.32s; }
        .typing-indicator span:nth-child(2) { animation-delay: -0.16s; }
        
        @keyframes bounce {
          0%, 80%, 100% { transform: scale(0); }
          40% { transform: scale(1); }
        }
      `}</style>
    </div>
  );
};

export default ChatInterface;
