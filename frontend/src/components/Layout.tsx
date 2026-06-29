import { NavLink, useNavigate, Outlet } from 'react-router-dom';
import {
  LayoutDashboard,
  Users,
  MessageSquare,
  FileText,
  Building2,
  LogOut,
  Globe,
  Droplets,
} from 'lucide-react';
import { useLanguage } from '../i18n/LanguageContext';
import './Layout.css';

const navItems = [
  { to: '/dashboard', icon: LayoutDashboard, key: 'dashboard' as const },
  { to: '/donors', icon: Users, key: 'donorDictionary' as const },
  { to: '/sms', icon: MessageSquare, key: 'sendSms' as const },
  { to: '/reports', icon: FileText, key: 'reports' as const },
  { to: '/hospitals', icon: Building2, key: 'hospitals' as const },
];

export default function Layout() {
  const { t, lang, setLang } = useLanguage();
  const navigate = useNavigate();
  const user = JSON.parse(localStorage.getItem('user') || '{}');

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    navigate('/login');
  };

  return (
    <div className="layout">
      <aside className="sidebar">
        <div className="mobile-topbar">
          <div className="sidebar-brand">
            <Droplets size={32} className="brand-icon" />
            <div>
              <h1>{t('appName')}</h1>
              <p>{t('appSubtitle')}</p>
            </div>
          </div>
          <div className="sidebar-footer">
            <button className="lang-toggle" onClick={() => setLang(lang === 'en' ? 'am' : 'en')}>
              <Globe size={18} />
              {lang === 'en' ? 'አማርኛ' : 'English'}
            </button>
            <div className="user-info">{user.fullName || user.username}</div>
            <button className="logout-btn" onClick={handleLogout}>
              <LogOut size={18} />
              {t('logout')}
            </button>
          </div>
        </div>
        <nav className="sidebar-nav">
          {navItems.map(({ to, icon: Icon, key }) => (
            <NavLink key={to} to={to} className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}>
              <Icon size={22} />
              <span>{t(key)}</span>
            </NavLink>
          ))}
        </nav>
      </aside>
      <main className="main-content">
        <Outlet />
      </main>
    </div>
  );
}
