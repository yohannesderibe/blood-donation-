import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { LanguageProvider } from './i18n/LanguageContext';
import Layout from './components/Layout';
import ProtectedRoute, { PublicRoute } from './components/ProtectedRoute';
import LoginPage from './pages/LoginPage';
import DashboardPage from './pages/DashboardPage';
import DonorsPage from './pages/DonorsPage';
import SmsPage from './pages/SmsPage';
import ReportsPage from './pages/ReportsPage';
import HospitalsPage from './pages/HospitalsPage';
import './components/Layout.css';

function App() {
  return (
    <LanguageProvider>
      <BrowserRouter>
        <Routes>
          <Route element={<PublicRoute />}>
            <Route path="/login" element={<LoginPage />} />
          </Route>
          <Route element={<ProtectedRoute />}>
            <Route element={<Layout />}>
              <Route path="/dashboard" element={<DashboardPage />} />
              <Route path="/donors" element={<DonorsPage />} />
              <Route path="/sms" element={<SmsPage />} />
              <Route path="/reports" element={<ReportsPage />} />
              <Route path="/hospitals" element={<HospitalsPage />} />
            </Route>
          </Route>
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </BrowserRouter>
    </LanguageProvider>
  );
}

export default App;
