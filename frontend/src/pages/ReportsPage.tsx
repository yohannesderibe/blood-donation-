import { useEffect, useState } from 'react';
import { Download, Trash2 } from 'lucide-react';
import api, { type ReportMetadata } from '../api/client';
import { useLanguage } from '../i18n/LanguageContext';

const REPORT_TYPES = [
  { value: 'DonorDirectory', labelKey: 'donorDirectory' as const },
  { value: 'DonationHistory', labelKey: 'donationHistory' as const },
  { value: 'SmsCampaigns', labelKey: 'smsCampaigns' as const },
];

export default function ReportsPage() {
  const { t } = useLanguage();
  const [reportType, setReportType] = useState('DonorDirectory');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [format, setFormat] = useState('csv');
  const [history, setHistory] = useState<ReportMetadata[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => {
    if (error) {
      const timer = setTimeout(() => setError(''), 4000);
      return () => clearTimeout(timer);
    }
  }, [error]);

  useEffect(() => {
    if (success) {
      const timer = setTimeout(() => setSuccess(''), 4000);
      return () => clearTimeout(timer);
    }
  }, [success]);

  const fetchHistory = () => {
    api.get<ReportMetadata[]>('/reports/history')
      .then(({ data }) => setHistory(data))
      .catch(console.error);
  };

  useEffect(() => { fetchHistory(); }, []);

  const handleDownload = async () => {
    setLoading(true);
    setError('');
    try {
      const response = await api.post('/reports/generate', {
        reportType,
        dateFrom: dateFrom || null,
        dateTo: dateTo || null,
        format,
      }, { responseType: 'blob' });

      const contentDisposition = response.headers['content-disposition'];
      let fileName = `report.${format}`;
      if (contentDisposition) {
        const match = contentDisposition.match(/filename="?(.+?)"?$/);
        if (match) fileName = match[1];
      }

      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', fileName);
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);

      fetchHistory();
    } catch {
      setError(t('error'));
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteSms = async (id: string) => {
    if (!confirm(t('confirmDeleteSms'))) return;
    try {
      await api.delete(`/sms/${id}`);
      setSuccess(t('success'));
      fetchHistory();
    } catch {
      setError(t('error'));
    }
  };

  const handleDeleteAllSms = async () => {
    if (!confirm(t('confirmDeleteAll'))) return;
    try {
      await api.delete('/sms/all');
      setSuccess(t('success'));
      fetchHistory();
    } catch {
      setError(t('error'));
    }
  };

  return (
    <div>
      <div className="page-header"><h2>{t('reports')}</h2></div>

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      <div className="grid-2">
        <div className="card">
          <h3 className="card-title">{t('download')}</h3>
          <div className="form-grid">
            <div className="form-group">
              <label>{t('reportType')}</label>
              <select value={reportType} onChange={(e) => setReportType(e.target.value)}>
                {REPORT_TYPES.map((r) => (
                  <option key={r.value} value={r.value}>{t(r.labelKey)}</option>
                ))}
              </select>
            </div>
            <div className="form-group">
              <label>{t('format')}</label>
              <select value={format} onChange={(e) => setFormat(e.target.value)}>
                <option value="csv">Excel (CSV)</option>
                <option value="pdf">PDF</option>
              </select>
            </div>
            <div className="form-group">
              <label>{t('dateFrom')}</label>
              <input type="date" value={dateFrom} onChange={(e) => setDateFrom(e.target.value)} />
            </div>
            <div className="form-group">
              <label>{t('dateTo')}</label>
              <input type="date" value={dateTo} onChange={(e) => setDateTo(e.target.value)} />
            </div>
          </div>
          <button className="btn btn-primary" style={{ marginTop: 20 }} onClick={handleDownload} disabled={loading}>
            <Download size={18} />
            {loading ? t('loading') : t('download')}
          </button>
        </div>

        <div className="card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
            <h3 className="card-title" style={{ margin: 0 }}>{t('reports')} - {t('smsHistory')}</h3>
            {history.length > 0 && (
              <button className="btn btn-secondary btn-sm" onClick={handleDeleteAllSms} style={{ color: '#dc3545', borderColor: '#dc3545' }}>
                <Trash2 size={14} /> {t('deleteAll')}
              </button>
            )}
          </div>
          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>{t('reportType')}</th>
                  <th>{t('format')}</th>
                  <th>{t('sentAt')}</th>
                  <th>{t('recipientCount')}</th>
                  <th>{t('actions')}</th>
                </tr>
              </thead>
              <tbody>
                {history.length === 0 ? (
                  <tr><td colSpan={5}>{t('noData')}</td></tr>
                ) : (
                  history.slice(0, 5).map((r) => (
                    <tr key={r.id}>
                      <td>{r.reportType}</td>
                      <td>{r.fileFormat}</td>
                      <td>{new Date(r.generatedAt).toLocaleString()}</td>
                      <td>{r.recordCount}</td>
                      <td>
                        <button className="btn btn-secondary btn-sm" onClick={() => handleDeleteSms(r.id)} style={{ color: '#dc3545', borderColor: '#dc3545' }}>
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
