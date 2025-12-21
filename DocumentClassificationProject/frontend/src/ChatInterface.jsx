import React, { useState, useRef, useEffect, useImperativeHandle, forwardRef } from 'react';
import axios from 'axios';
import { Send, Bot, User, AlertCircle } from 'lucide-react';
import { Button } from './components/ui/Button';
import { cn } from './lib/utils';

const ChatInterface = forwardRef(({ selectedDocuments }, ref) => {
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

  const sendMessage = async (text) => {
    if (!text.trim()) return;

    const userMessage = { id: Date.now(), text: text, sender: 'user' };
    setMessages(prev => [...prev, userMessage]);
    setLoading(true);

    try {
      const response = await axios.post('http://localhost:7071/api/Chat', {
        query: userMessage.text,
        fileNames: selectedDocuments
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

  useImperativeHandle(ref, () => ({
    sendMessage
  }));

  const handleSend = (e) => {
    e.preventDefault();
    sendMessage(input);
    setInput('');
  };

  return (
    <div className="flex flex-col h-full bg-card rounded-lg border shadow-sm overflow-hidden">
      <div className="flex-1 overflow-y-auto p-4 space-y-4">
        {messages.map((msg) => (
          <div
            key={msg.id}
            className={cn(
              "flex w-full",
              msg.sender === 'user' ? "justify-end" : "justify-start"
            )}
          >
            <div className={cn(
              "flex max-w-[80%] rounded-lg p-3 gap-3",
              msg.sender === 'user'
                ? "bg-primary text-primary-foreground"
                : msg.isError
                  ? "bg-destructive/10 text-destructive border border-destructive/20"
                  : "bg-muted text-foreground"
            )}>
              <div className="flex-shrink-0 mt-1">
                {msg.sender === 'user' ? (
                  <User className="h-4 w-4 opacity-70" />
                ) : msg.isError ? (
                  <AlertCircle className="h-4 w-4" />
                ) : (
                  <Bot className="h-4 w-4 opacity-70" />
                )}
              </div>
              <div className="text-sm leading-relaxed whitespace-pre-wrap">
                {msg.text}
              </div>
            </div>
          </div>
        ))}
        {loading && (
          <div className="flex justify-start w-full">
            <div className="flex max-w-[80%] rounded-lg p-4 bg-muted text-foreground gap-3 items-center">
              <Bot className="h-4 w-4 opacity-70" />
              <div className="flex space-x-1">
                <div className="w-2 h-2 bg-current rounded-full animate-bounce [animation-delay:-0.3s]"></div>
                <div className="w-2 h-2 bg-current rounded-full animate-bounce [animation-delay:-0.15s]"></div>
                <div className="w-2 h-2 bg-current rounded-full animate-bounce"></div>
              </div>
            </div>
          </div>
        )}
        <div ref={messagesEndRef} />
      </div>

      <div className="p-4 border-t bg-card/50 backdrop-blur-sm">
        <form onSubmit={handleSend} className="flex gap-2">
          <input
            type="text"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            placeholder="Type your question..."
            disabled={loading}
            className="flex-1 min-w-0 rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
          />
          <Button type="submit" disabled={loading || !input.trim()}>
            <Send className="h-4 w-4 mr-2" />
            Send
          </Button>
        </form>
      </div>
    </div>
  );
});

ChatInterface.displayName = "ChatInterface";

export default ChatInterface;
