import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Droplets, Globe, Eye, EyeOff } from 'lucide-react';
import api, { type LoginResponse } from '../api/client';
import { useLanguage } from '../i18n/LanguageContext';

export default function LoginPage() {
  const { t, lang, setLang } = useLanguage();
  const navigate = useNavigate();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const { data } = await api.post<LoginResponse>('/auth/login', { username, password });
      localStorage.setItem('token', data.token);
      localStorage.setItem('user', JSON.stringify({ username: data.username, fullName: data.fullName, role: data.role }));
      navigate('/dashboard');
    } catch (err: any) {
      if (err.response?.data?.message) {
        setError(err.response.data.message);
      } else if (err.response?.data?.error) {
        setError(`${err.response.data.error}: ${err.response.data.message || ''}`);
      } else {
        setError(lang === 'en' ? 'Invalid username or password' : 'የተሳሳተ የተጠቃሚ ስም ወይም የይለፍ ቃል');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-page">
      <div className="login-card">
        <button
          className="lang-toggle"
          style={{ marginBottom: 16, width: '100%', color: '#333', borderColor: '#dee2e6' }}
          onClick={() => setLang(lang === 'en' ? 'am' : 'en')}
        >
          <Globe size={18} />
          {lang === 'en' ? 'አማርኛ' : 'English'}
        </button>
        <div className="login-brand">
          <Droplets size={48} color="#8b0000" style={{ margin: '0 auto' }} />
          <h1>{t('appName')}</h1>
          <p>{t('appSubtitle')}</p>
        </div>
        {error && <div className="alert alert-error">{error}</div>}
        <form onSubmit={handleSubmit}>
          <div className="form-group" style={{ marginBottom: 16 }}>
            <label>{t('username')}</label>
            <input value={username} onChange={(e) => setUsername(e.target.value)} required autoComplete="username" />
          </div>
          <div className="form-group" style={{ marginBottom: 24 }}>
            <label>{t('password')}</label>
            <div className="password-input-container">
              <input type={showPassword ? "text" : "password"} value={password} onChange={(e) => setPassword(e.target.value)} required autoComplete="current-password" />
              <button type="button" className="password-toggle-btn" onClick={() => setShowPassword(!showPassword)}>
                {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
              </button>
            </div>
          </div>
          <button type="submit" className="btn btn-primary" style={{ width: '100%' }} disabled={loading}>
            {loading ? t('loading') : t('login')}
          </button>
        </form>
      </div>
    </div>
  );
}
