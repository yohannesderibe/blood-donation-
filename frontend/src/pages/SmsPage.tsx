import { useEffect, useState } from 'react';
import { Send, RefreshCw, Trash2 } from 'lucide-react';
import api, { type SmsBalance, type SmsLog, type DonorListItem, type PagedResult } from '../api/client';
import { useLanguage } from '../i18n/LanguageContext';

const RECIPIENT_TYPES = [
  { value: 'All', labelKey: 'allDonors' as const },
  { value: 'Selected', labelKey: 'selectedDonors' as const },
  { value: 'SundaySchool', labelKey: 'sundaySchoolGroup' as const },
  { value: 'Eligible', labelKey: 'eligibleGroup' as const },
];

export default function SmsPage() {
  const { t } = useLanguage();
  const [balance, setBalance] = useState<SmsBalance | null>(null);
  const [history, setHistory] = useState<SmsLog[]>([]);
  const [recipientType, setRecipientType] = useState('All');
  const [message, setMessage] = useState('');
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [donors, setDonors] = useState<DonorListItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => {
    if (error) {
      const t = setTimeout(() => setError(''), 4000);
      return () => clearTimeout(t);
    }
  }, [error]);

  useEffect(() => {
    if (success) {
      const t = setTimeout(() => setSuccess(''), 4000);
      return () => clearTimeout(t);
    }
  }, [success]);

  const fetchData = async () => {
    try {
      const [bal, hist] = await Promise.all([
        api.get<SmsBalance>('/sms/balance'),
        api.get<SmsLog[]>('/sms/history'),
      ]);
      setBalance(bal.data);
      setHistory(hist.data);
    } catch {
      setError(t('error'));
    }
  };

  useEffect(() => { fetchData(); }, [t]);

  useEffect(() => {
    if (recipientType === 'Selected') {
      api.get<PagedResult<DonorListItem>>('/donors', { params: { pageSize: 100 } })
        .then(({ data }) => setDonors(data.items))
        .catch(console.error);
    }
  }, [recipientType]);

  const handleSend = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!message.trim()) return;
    setLoading(true);
    setError('');
    setSuccess('');
    try {
      const response = await api.post<SmsLog>('/sms/send', {
        recipientType,
        message: message.trim(),
        donorIds: recipientType === 'Selected' ? selectedIds : null,
      });
      
      if (response.data.status === 'Failed') {
        setError(t('error') + ': ' + (response.data.errorMessage || 'Unknown error'));
        console.error('Failed to send SMS:', response.data);
      } else {
        setSuccess(t('success'));
        setMessage('');
        setSelectedIds([]);
      }
      fetchData();
    } catch (err: any) {
      console.error('API Error sending SMS:', err.response?.data || err);
      setError(t('error') + (err.response?.data?.details ? `: ${err.response.data.details}` : ''));
    } finally {
      setLoading(false);
    }
  };

  const handleRetry = async (id: string) => {
    try {
      await api.post(`/sms/${id}/retry`);
      setSuccess(t('success'));
      fetchData();
    } catch {
      setError(t('error'));
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm(t('confirmDeleteSms'))) return;
    try {
      await api.delete(`/sms/${id}`);
      setSuccess(t('success'));
      fetchData();
    } catch {
      setError(t('error'));
    }
  };

  const handleDeleteAll = async () => {
    if (!confirm(t('confirmDeleteAll'))) return;
    try {
      await api.delete('/sms/all');
      setSuccess(t('success'));
      fetchData();
    } catch {
      setError(t('error'));
    }
  };

  const toggleDonor = (id: string) => {
    setSelectedIds((prev) => prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]);
  };

  return (
    <div>
      <div className="page-header"><h2>{t('sendSms')}</h2></div>

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      <div className="stats-grid" style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))' }}>
        <div className="stat-card">
          <h3>{t('balance')}</h3>
          <div className="value" style={{ fontSize: '1.5rem' }}>ETB {balance?.balanceEtb?.toFixed(3) ?? '0'}</div>
        </div>
        <div className="stat-card eligible">
          <h3>{t('estimatedMessages')}</h3>
          <div className="value" style={{ fontSize: '1.5rem' }}>{balance?.estimatedMessages ?? 0}</div>
        </div>
      </div>

      <div className="grid-2">
        <div className="card">
          <h3 className="card-title">{t('sendSms')}</h3>
          <form onSubmit={handleSend}>
            <div className="form-group" style={{ marginBottom: 16 }}>
              <label>{t('smsRecipients')}</label>
              <select value={recipientType} onChange={(e) => setRecipientType(e.target.value)}>
                {RECIPIENT_TYPES.map((r) => (
                  <option key={r.value} value={r.value}>{t(r.labelKey)}</option>
                ))}
              </select>
            </div>

            <div className="form-group" style={{ marginBottom: 20 }}>
              <label>{t('message')}</label>
              <textarea value={message} onChange={(e) => setMessage(e.target.value)} required rows={5} maxLength={480} />
              <small style={{ color: 'var(--text-muted)' }}>{message.length}/480</small>
            </div>

            <button type="submit" className="btn btn-primary" disabled={loading}>
              <Send size={18} />
              {loading ? t('loading') : t('send')}
            </button>
          </form>
        </div>

        {recipientType === 'Selected' && (
          <div className="card" style={{ display: 'flex', flexDirection: 'column' }}>
            <h3 className="card-title">{t('selectedDonors')}</h3>
            <div style={{ flex: 1, maxHeight: 350, overflowY: 'auto', border: '1px solid var(--border)', borderRadius: 8, padding: 12 }}>
              {donors.map((d) => (
                <label key={d.id} style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '6px 0', cursor: 'pointer' }}>
                  <input type="checkbox" checked={selectedIds.includes(d.id)} onChange={() => toggleDonor(d.id)} />
                  {d.fullName} ({d.phone})
                </label>
              ))}
            </div>
          </div>
        )}

        <div className="card" style={recipientType === 'Selected' ? { gridColumn: '1 / -1' } : {}}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
            <h3 className="card-title" style={{ margin: 0 }}>{t('smsHistory')}</h3>
            {history.length > 0 && (
              <button className="btn btn-secondary btn-sm" onClick={handleDeleteAll} style={{ color: '#dc3545', borderColor: '#dc3545' }}>
                <Trash2 size={14} /> {t('deleteAll')}
              </button>
            )}
          </div>
          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>{t('sentAt')}</th>
                  <th>{t('recipientCount')}</th>
                  <th>{t('message')}</th>
                  <th>{t('status')}</th>
                  <th>{t('actions')}</th>
                </tr>
              </thead>
              <tbody>
                {history.length === 0 ? (
                  <tr><td colSpan={5}>{t('noData')}</td></tr>
                ) : (
                  history.map((log) => (
                    <tr key={log.id}>
                      <td>{new Date(log.sentAt).toLocaleString()}</td>
                      <td>{log.recipientCount}</td>
                      <td>{log.messageContent.length > 50 ? log.messageContent.slice(0, 50) + '...' : log.messageContent}</td>
                      <td>
                        <span className={`badge ${log.status === 'Sent' ? 'badge-success' : 'badge-danger'}`}>
                          {log.status}
                        </span>
                      </td>
                      <td style={{ display: 'flex', gap: 6 }}>
                        {log.status === 'Failed' && (
                          <button className="btn btn-secondary btn-sm" onClick={() => handleRetry(log.id)}>
                            <RefreshCw size={14} /> {t('retry')}
                          </button>
                        )}
                        <button className="btn btn-secondary btn-sm" onClick={() => handleDelete(log.id)} style={{ color: '#dc3545', borderColor: '#dc3545' }}>
                          <Trash2 size={14} /> {t('delete')}
                        </button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
}
