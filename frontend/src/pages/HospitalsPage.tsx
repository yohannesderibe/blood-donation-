import { useEffect, useState } from 'react';
import { Plus, Pencil, Trash2, X } from 'lucide-react';
import api, { type Hospital } from '../api/client';
import { useLanguage } from '../i18n/LanguageContext';

interface HospitalForm {
  name: string;
  contactPerson: string;
  phone: string;
  email: string;
  notes: string;
  isActive: boolean;
}

const emptyForm: HospitalForm = { name: '', contactPerson: '', phone: '', email: '', notes: '', isActive: true };

export default function HospitalsPage() {
  const { t } = useLanguage();
  const [hospitals, setHospitals] = useState<Hospital[]>([]);
  const [showForm, setShowForm] = useState(false);
  const [editId, setEditId] = useState<string | null>(null);
  const [form, setForm] = useState<HospitalForm>(emptyForm);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const fetchHospitals = async () => {
    try {
      const { data } = await api.get<Hospital[]>('/hospitals', { params: { activeOnly: false } });
      setHospitals(data);
    } catch {
      setError(t('error'));
    }
  };

  useEffect(() => { fetchHospitals(); }, [t]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    try {
      if (editId) {
        await api.put(`/hospitals/${editId}`, form);
      } else {
        await api.post('/hospitals', form);
      }
      setSuccess(t('success'));
      setShowForm(false);
      setEditId(null);
      setForm(emptyForm);
      fetchHospitals();
    } catch {
      setError(t('error'));
    }
  };

  const handleEdit = (h: Hospital) => {
    setEditId(h.id);
    setForm({
      name: h.name,
      contactPerson: h.contactPerson || '',
      phone: h.phone || '',
      email: h.email || '',
      notes: h.notes || '',
      isActive: h.isActive,
    });
    setShowForm(true);
  };

  const handleDelete = async (id: string) => {
    if (!confirm(t('confirmDelete'))) return;
    try {
      await api.delete(`/hospitals/${id}`);
      fetchHospitals();
    } catch {
      setError(t('error'));
    }
  };

  const updateForm = (field: keyof HospitalForm, value: string | boolean) => {
    setForm((f) => ({ ...f, [field]: value }));
  };

  return (
    <div>
      <div className="page-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 12 }}>
        <h2>{t('hospitals')}</h2>
        <button className="btn btn-primary" onClick={() => { setShowForm(!showForm); setEditId(null); setForm(emptyForm); }}>
          {showForm ? <X size={18} /> : <Plus size={18} />}
          {showForm ? t('cancel') : t('addHospital')}
        </button>
      </div>

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      {showForm && (
        <div className="card">
          <h3 className="card-title">{editId ? t('editHospital') : t('addHospital')}</h3>
          <form onSubmit={handleSubmit}>
            <div className="form-grid">
              <div className="form-group">
                <label>{t('hospitalName')} *</label>
                <input value={form.name} onChange={(e) => updateForm('name', e.target.value)} required />
              </div>
              <div className="form-group">
                <label>{t('contactPerson')}</label>
                <input value={form.contactPerson} onChange={(e) => updateForm('contactPerson', e.target.value)} />
              </div>
              <div className="form-group">
                <label>{t('phone')}</label>
                <input value={form.phone} onChange={(e) => updateForm('phone', e.target.value)} />
              </div>
              <div className="form-group">
                <label>{t('email')}</label>
                <input type="email" value={form.email} onChange={(e) => updateForm('email', e.target.value)} />
              </div>
              <div className="form-group" style={{ gridColumn: '1 / -1' }}>
                <label>{t('notes')}</label>
                <textarea value={form.notes} onChange={(e) => updateForm('notes', e.target.value)} rows={3} />
              </div>
              {editId && (
                <div className="form-group">
                  <label>{t('status')}</label>
                  <select value={form.isActive ? 'active' : 'inactive'} onChange={(e) => updateForm('isActive', e.target.value === 'active')}>
                    <option value="active">{t('active')}</option>
                    <option value="inactive">{t('inactive')}</option>
                  </select>
                </div>
              )}
            </div>
            <div style={{ marginTop: 20, display: 'flex', gap: 12 }}>
              <button type="submit" className="btn btn-primary">{t('save')}</button>
              <button type="button" className="btn btn-secondary" onClick={() => { setShowForm(false); setEditId(null); }}>{t('cancel')}</button>
            </div>
          </form>
        </div>
      )}

      <div className="card">
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>{t('hospitalName')}</th>
                <th>{t('contactPerson')}</th>
                <th>{t('phone')}</th>
                <th>{t('email')}</th>
                <th>{t('status')}</th>
                <th>{t('actions')}</th>
              </tr>
            </thead>
            <tbody>
              {hospitals.length === 0 ? (
                <tr><td colSpan={6}>{t('noData')}</td></tr>
              ) : (
                hospitals.map((h) => (
                  <tr key={h.id}>
                    <td>{h.name}</td>
                    <td>{h.contactPerson || '-'}</td>
                    <td>{h.phone || '-'}</td>
                    <td>{h.email || '-'}</td>
                    <td>
                      <span className={`badge ${h.isActive ? 'badge-success' : 'badge-danger'}`}>
                        {h.isActive ? t('active') : t('inactive')}
                      </span>
                    </td>
                    <td>
                      <div className="action-btns">
                        <button className="icon-btn view" onClick={() => handleEdit(h)}><Pencil size={18} /></button>
                        <button className="icon-btn edit" onClick={() => handleDelete(h.id)}><Trash2 size={18} /></button>
                      </div>
                    </td>
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
