import React from 'react';
import { LayoutDashboard, MessageSquare, FileText, Settings } from 'lucide-react';
import { cn } from '../lib/utils';

const SidebarItem = ({ icon: Icon, label, active, onClick }) => (
    <button
        onClick={onClick}
        className={cn(
            "flex items-center w-full px-4 py-3 text-sm font-medium rounded-lg transition-colors",
            active
                ? "bg-primary/10 text-primary"
                : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
        )}
    >
        <Icon className="w-5 h-5 mr-3" />
        {label}
    </button>
);

const Layout = ({ children, activeTab, onTabChange }) => {
    return (
        <div className="flex h-screen bg-background overflow-hidden">
            {/* Sidebar */}
            <aside className="w-64 border-r bg-card/50 backdrop-blur-xl hidden md:flex flex-col">
                <div className="p-6">
                    <h1 className="text-2xl font-bold bg-gradient-to-r from-primary to-purple-600 bg-clip-text text-transparent">
                        DocMind AI
                    </h1>
                    <p className="text-xs text-muted-foreground mt-1">Intelligent Document Analysis</p>
                </div>

                <nav className="flex-1 px-4 space-y-2">
                    <SidebarItem
                        icon={LayoutDashboard}
                        label="Dashboard"
                        active={activeTab === 'dashboard'}
                        onClick={() => onTabChange('dashboard')}
                    />
                    <SidebarItem
                        icon={MessageSquare}
                        label="Chat"
                        active={activeTab === 'chat'}
                        onClick={() => onTabChange('chat')}
                    />
                    <SidebarItem
                        icon={FileText}
                        label="Documents"
                        active={activeTab === 'documents'}
                        onClick={() => onTabChange('documents')}
                    />
                </nav>

                <div className="p-4 border-t">
                    <SidebarItem
                        icon={Settings}
                        label="Settings"
                        active={activeTab === 'settings'}
                        onClick={() => onTabChange('settings')}
                    />
                </div>
            </aside>

            {/* Main Content */}
            <main className="flex-1 overflow-auto relative">
                <div className="absolute inset-0 bg-gradient-to-br from-primary/5 via-transparent to-purple-500/5 pointer-events-none" />
                <div className="relative z-10 p-8 max-w-7xl mx-auto">
                    {children}
                </div>
            </main>
        </div>
    );
};

export default Layout;
