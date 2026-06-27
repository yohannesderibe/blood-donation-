import { useEffect, useState } from 'react';
import { PieChart, Pie, Cell, ResponsiveContainer, Legend, Tooltip } from 'recharts';
import api, { type DashboardSummary, type BloodTypeDistribution, type RecentDonor, type Notification } from '../api/client';
import { useLanguage } from '../i18n/LanguageContext';

const COLORS = ['#8b0000', '#c41e3a', '#198754', '#0dcaf0', '#ffc107', '#6f42c1', '#fd7e14', '#20c997', '#adb5bd'];

export default function DashboardPage() {
  const { t, lang } = useLanguage();
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [bloodTypes, setBloodTypes] = useState<BloodTypeDistribution[]>([]);
  const [recentDonors, setRecentDonors] = useState<RecentDonor[]>([]);
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchDashboard = async () => {
      try {
        const [s, b, r, n] = await Promise.allSettled([
          api.get<DashboardSummary>('/dashboard/summary'),
          api.get<BloodTypeDistribution[]>('/dashboard/blood-types'),
          api.get<RecentDonor[]>('/dashboard/recent-donors'),
          api.get<Notification[]>('/dashboard/notifications'),
        ]);

        if (s.status === 'fulfilled') setSummary(s.value.data);
        else console.error('Failed to fetch summary:', s.reason);

        if (b.status === 'fulfilled') setBloodTypes(b.value.data);
        else console.error('Failed to fetch blood types:', b.reason);

        if (r.status === 'fulfilled') setRecentDonors(r.value.data);
        else console.error('Failed to fetch recent donors:', r.reason);

        if (n.status === 'fulfilled') setNotifications(n.value.data);
        else console.error('Failed to fetch notifications:', n.reason);
      } finally {
        setLoading(false);
      }
    };

    fetchDashboard();
  }, []);

  if (loading) return <div className="page-header"><h2>{t('loading')}</h2></div>;

  const chartData = bloodTypes.map((b) => ({
    name: b.bloodType === 'Unknown' ? t('unknown') : b.bloodType,
    value: b.count,
  }));

  return (
    <div>
      <div className="page-header">
        <h2>{t('dashboard')}</h2>
      </div>

      <div className="stats-grid">
        <div className="stat-card">
          <h3>{t('totalDonors')}</h3>
          <div className="value">{summary?.totalDonors ?? 0}</div>
        </div>
        <div className="stat-card eligible">
          <h3>{t('eligibleDonors')}</h3>
          <div className="value">{summary?.eligibleDonors ?? 0}</div>
        </div>
        <div className="stat-card not-eligible">
          <h3>{t('nonEligibleDonors')}</h3>
          <div className="value">{summary?.nonEligibleDonors ?? 0}</div>
        </div>
      </div>

      <div className="grid-2">
        <div className="card">
          <h3 className="card-title">{t('bloodTypeDistribution')}</h3>
          {chartData.length > 0 ? (
            <ResponsiveContainer width="100%" height={280}>
              <PieChart>
                <Pie data={chartData} dataKey="value" nameKey="name" cx="50%" cy="50%" outerRadius={90} label>
                  {chartData.map((_, i) => (
                    <Cell key={i} fill={COLORS[i % COLORS.length]} />
                  ))}
                </Pie>
                <Tooltip />
                <Legend />
              </PieChart>
            </ResponsiveContainer>
          ) : (
            <p>{t('noData')}</p>
          )}
        </div>

        <div className="card">
          <h3 className="card-title">{t('notifications')}</h3>
          {notifications.length === 0 ? (
            <p>{t('noData')}</p>
          ) : (
            notifications.map((n) => (
              <div key={n.id} className={`notification-item ${n.notificationType.toLowerCase()}`}>
                <h4>{lang === 'am' && n.titleAm ? n.titleAm : n.titleEn}</h4>
                <p>{lang === 'am' && n.messageAm ? n.messageAm : n.messageEn}</p>
              </div>
            ))
          )}
        </div>
      </div>

      <div className="card">
        <h3 className="card-title">{t('recentDonors')}</h3>
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>{t('name')}</th>
                <th>{t('christianName')}</th>
                <th>{t('phone')}</th>
                <th>{t('bloodType')}</th>
              </tr>
            </thead>
            <tbody>
              {recentDonors.length === 0 ? (
                <tr><td colSpan={4}>{t('noData')}</td></tr>
              ) : (
                recentDonors.map((d) => (
                  <tr key={d.id}>
                    <td>{d.fullName}</td>
                    <td>{d.christianName}</td>
                    <td>{d.phone}</td>
                    <td>{d.bloodType}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
