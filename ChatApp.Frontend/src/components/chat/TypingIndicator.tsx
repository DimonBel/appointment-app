import React from 'react';

export const TypingIndicator: React.FC = () => {
  return (
    <div className="typing-indicator">
      <div className="typing-dot" style={{ '--i': 0 } as React.CSSProperties}></div>
      <div className="typing-dot" style={{ '--i': 1 } as React.CSSProperties}></div>
      <div className="typing-dot" style={{ '--i': 2 } as React.CSSProperties}></div>
    </div>
  );
};