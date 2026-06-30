import { useEffect, useState, useCallback } from 'react';
import { Eye, EyeOff, Trash2, CheckCircle, Plus, X } from 'lucide-react';
import api, { BLOOD_TYPES, type DonorListItem, type DonorDetail, type PagedResult } from '../api/client';
import { useLanguage } from '../i18n/LanguageContext';

interface DonorForm {
  fullName: string;
  christianName: string;
  phone: string;
  bloodType: string;
  gender: string;
  isSundaySchoolMember: boolean;
  isFirstTimeDonor: boolean;
  lastDonationDate: string;
  previousDonationCount: number;
  howHeardAboutUs: string;
  howHeardOther: string;
}

const HOW_HEARD_OPTIONS = [
  { value: 'TikTok', label: 'TikTok' },
  { value: 'SMS', label: 'SMS' },
  { value: 'Poster', label: 'Poster' },
  { value: 'Other', label: 'Other' },
];

const emptyForm: DonorForm = {
  fullName: '', christianName: '', phone: '', bloodType: 'Unknown',
  gender: 'Male', isSundaySchoolMember: false, isFirstTimeDonor: true,
  lastDonationDate: '', previousDonationCount: 0, howHeardAboutUs: '', howHeardOther: '',
};

export default function DonorsPage() {
  const { t } = useLanguage();
  const [donors, setDonors] = useState<DonorListItem[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [search, setSearch] = useState('');
  const [bloodType, setBloodType] = useState('All');
  const [eligibility, setEligibility] = useState('');
  const [sundaySchool, setSundaySchool] = useState('');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<DonorForm>(emptyForm);
  const [viewDonor, setViewDonor] = useState<DonorDetail | null>(null);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [loading, setLoading] = useState(false);

  const fetchDonors = useCallback(async () => {
    setLoading(true);
    try {
      const params: Record<string, string | number | boolean> = { page, pageSize: 10 };
      if (search) params.search = search;
      if (bloodType !== 'All') params.bloodType = bloodType;
      if (eligibility !== '') params.isEligible = eligibility === 'true';
      if (sundaySchool !== '') params.isSundaySchoolMember = sundaySchool === 'true';
      if (dateFrom) params.lastDonationFrom = dateFrom;
      if (dateTo) params.lastDonationTo = dateTo;

      const { data } = await api.get<PagedResult<DonorListItem>>('/donors', { params });
      setDonors(data.items);
      setTotalPages(data.totalPages);
    } catch {
      setError(t('error'));
    } finally {
      setLoading(false);
    }
  }, [page, search, bloodType, eligibility, sundaySchool, dateFrom, dateTo, t]);

  useEffect(() => { fetchDonors(); }, [fetchDonors]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    try {
      const howHeard = form.howHeardAboutUs === 'Other'
        ? form.howHeardOther || 'Other'
        : form.howHeardAboutUs || null;
      await api.post('/donors', {
        ...form,
        lastDonationDate: form.isFirstTimeDonor ? null : form.lastDonationDate || null,
        previousDonationCount: form.isFirstTimeDonor ? 0 : form.previousDonationCount,
        howHeardAboutUs: howHeard,
      });
      setSuccess(t('success'));
      setShowForm(false);
      setForm(emptyForm);
      fetchDonors();
    } catch {
      setError(t('error'));
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm(t('confirmDelete'))) return;
    try {
      await api.delete(`/donors/${id}`);
      fetchDonors();
    } catch {
      setError(t('error'));
    }
  };

  const handleView = async (id: string) => {
    try {
      const { data } = await api.get<DonorDetail>(`/donors/${id}`);
      setViewDonor(data);
    } catch {
      setError(t('error'));
    }
  };

  const handleDonatedToday = async (id: string) => {
    try {
      await api.post(`/donors/${id}/donate-today`, {});
      setSuccess(t('success'));
      fetchDonors();
    } catch {
      setError(t('error'));
    }
  };

  const updateForm = (field: keyof DonorForm, value: string | boolean | number) => {
    setForm((f) => ({ ...f, [field]: value }));
  };

  return (
    <div>
      <div className="page-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 12 }}>
        <h2>{t('donorDictionary')}</h2>
        <button className="btn btn-primary" onClick={() => setShowForm(!showForm)}>
          {showForm ? <X size={18} /> : <Plus size={18} />}
          {showForm ? t('cancel') : t('addDonor')}
        </button>
      </div>

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      {showForm && (
        <div className="modal-overlay" onClick={() => setShowForm(false)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>{t('addDonor')}</h3>
              <button className="btn btn-secondary btn-sm" onClick={() => setShowForm(false)}>{t('close')}</button>
            </div>
            <form onSubmit={handleSubmit}>
              <div className="form-grid">
                <div className="form-group">
                  <label>{t('fullName')} *<span className="amharic">{t('fullName')}</span></label>
                  <input value={form.fullName} onChange={(e) => updateForm('fullName', e.target.value)} required />
                </div>
                <div className="form-group">
                  <label>{t('christianName')} *<span className="amharic">{t('christianName')}</span></label>
                  <input value={form.christianName} onChange={(e) => updateForm('christianName', e.target.value)} required />
                </div>
                <div className="form-group">
                  <label>{t('phone')} *<span className="amharic">{t('phone')}</span></label>
                  <input value={form.phone} onChange={(e) => updateForm('phone', e.target.value)} required />
                </div>

                <div className="form-group">
                  <label>{t('bloodType')} *</label>
                  <select value={form.bloodType} onChange={(e) => updateForm('bloodType', e.target.value)} required>
                    {BLOOD_TYPES.map((bt) => <option key={bt} value={bt}>{bt === 'Unknown' ? t('unknown') : bt}</option>)}
                  </select>
                </div>
                <div className="form-group">
                  <label>{t('gender')}</label>
                  <select value={form.gender} onChange={(e) => updateForm('gender', e.target.value)}>
                    <option value="Male">{t('male')}</option>
                    <option value="Female">{t('female')}</option>
                  </select>
                </div>
                <div className="form-group">
                  <label>{t('sundaySchool')}</label>
                  <select value={form.isSundaySchoolMember ? 'yes' : 'no'} onChange={(e) => updateForm('isSundaySchoolMember', e.target.value === 'yes')}>
                    <option value="yes">{t('yes')}</option>
                    <option value="no">{t('no')}</option>
                  </select>
                </div>

                <div className="form-group">
                  <label>{t('firstTimeDonor')}</label>
                  <select value={form.isFirstTimeDonor ? 'yes' : 'no'} onChange={(e) => updateForm('isFirstTimeDonor', e.target.value === 'yes')}>
                    <option value="yes">{t('yes')}</option>
                    <option value="no">{t('no')}</option>
                  </select>
                </div>
                {!form.isFirstTimeDonor && (
                  <>
                    <div className="form-group">
                      <label>{t('lastDonationDate')}</label>
                      <input type="date" value={form.lastDonationDate} onChange={(e) => updateForm('lastDonationDate', e.target.value)} />
                    </div>
                    <div className="form-group">
                      <label>{t('previousDonationCount')}</label>
                      <input type="number" min={0} value={form.previousDonationCount} onChange={(e) => updateForm('previousDonationCount', +e.target.value)} />
                    </div>
                  </>
                )}
                <div className="form-group" style={{ gridColumn: '1 / -1' }}>
                  <label>{t('howHeard')}</label>
                  <select
                    value={form.howHeardAboutUs}
                    onChange={(e) => {
                      updateForm('howHeardAboutUs', e.target.value);
                      if (e.target.value !== 'Other') updateForm('howHeardOther', '');
                    }}
                  >
                    <option value="">-- Select --</option>
                    {HOW_HEARD_OPTIONS.map((opt) => (
                      <option key={opt.value} value={opt.value}>{opt.label}</option>
                    ))}
                  </select>
                  {form.howHeardAboutUs === 'Other' && (
                    <input
                      style={{ marginTop: 8 }}
                      placeholder="Please describe..."
                      value={form.howHeardOther}
                      onChange={(e) => updateForm('howHeardOther', e.target.value)}
                    />
                  )}
                </div>
              </div>
              <div style={{ marginTop: 20, display: 'flex', gap: 12 }}>
                <button type="submit" className="btn btn-primary">{t('save')}</button>
                <button type="button" className="btn btn-secondary" onClick={() => setShowForm(false)}>{t('cancel')}</button>
              </div>
            </form>
          </div>
        </div>
      )}

      <div className="card">
        <div className="filters-bar">
          <input className="search-input" placeholder={t('search')} value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }} />
          <select value={bloodType} onChange={(e) => { setBloodType(e.target.value); setPage(1); }}>
            <option value="All">{t('filterBloodType')}: {t('all')}</option>
            {BLOOD_TYPES.map((bt) => <option key={bt} value={bt}>{bt}</option>)}
          </select>
          <select value={eligibility} onChange={(e) => { setEligibility(e.target.value); setPage(1); }}>
            <option value="">{t('filterEligibility')}: {t('all')}</option>
            <option value="true">{t('eligible')}</option>
            <option value="false">{t('notEligible')}</option>
          </select>
          <select value={sundaySchool} onChange={(e) => { setSundaySchool(e.target.value); setPage(1); }}>
            <option value="">{t('filterSundaySchool')}: {t('all')}</option>
            <option value="true">{t('yes')}</option>
            <option value="false">{t('no')}</option>
          </select>
          <input type="date" value={dateFrom} onChange={(e) => { setDateFrom(e.target.value); setPage(1); }} title={t('filterDateFrom')} />
          <input type="date" value={dateTo} onChange={(e) => { setDateTo(e.target.value); setPage(1); }} title={t('filterDateTo')} />
        </div>

        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>{t('name')}</th>
                <th>{t('christianName')}</th>
                <th>{t('phone')}</th>
                <th>{t('bloodType')}</th>
                <th>{t('lastDonation')}</th>
                <th>{t('eligibilityStatus')}</th>
                <th>{t('actions')}</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr><td colSpan={7}>{t('loading')}</td></tr>
              ) : donors.length === 0 ? (
                <tr><td colSpan={7}>{t('noData')}</td></tr>
              ) : (
                donors.map((d) => (
                  <tr key={d.id}>
                    <td>{d.fullName}</td>
                    <td>{d.christianName}</td>
                    <td>{d.phone}</td>
                    <td>{d.bloodType}</td>
                    <td>{d.lastDonationDate ? new Date(d.lastDonationDate).toLocaleDateString() : '-'}</td>
                    <td>
                      <span className={`badge ${d.isEligible ? 'badge-success' : 'badge-danger'}`}>
                        {d.isEligible ? t('eligible') : t('notEligible')}
                      </span>
                    </td>
                    <td>
                      <div className="action-btns">
                        <button className="icon-btn view" title={t('view')} onClick={() => handleView(d.id)}><Eye size={18} /></button>
                        <button className="icon-btn edit" title={t('delete')} onClick={() => handleDelete(d.id)}><Trash2 size={18} /></button>
                        <button className="icon-btn donate" title={t('donatedToday')} onClick={() => handleDonatedToday(d.id)}><CheckCircle size={18} /></button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        <div className="pagination">
          <button className="btn btn-secondary btn-sm" disabled={page <= 1} onClick={() => setPage(page - 1)}>{t('previous')}</button>
          <span>{t('page')} {page} {t('of')} {totalPages}</span>
          <button className="btn btn-secondary btn-sm" disabled={page >= totalPages} onClick={() => setPage(page + 1)}>{t('next')}</button>
        </div>
      </div>

      {viewDonor && (
        <div className="modal-overlay" onClick={() => setViewDonor(null)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h3>{t('donorDetails')}</h3>
              <button className="btn btn-secondary btn-sm" onClick={() => setViewDonor(null)}>{t('close')}</button>
            </div>
            <div className="form-grid">
              <div><strong>{t('fullName')}:</strong> {viewDonor.fullName}</div>
              <div><strong>{t('christianName')}:</strong> {viewDonor.christianName}</div>
              <div><strong>{t('phone')}:</strong> {viewDonor.phone}</div>

              <div><strong>{t('bloodType')}:</strong> {viewDonor.bloodType}</div>
              <div><strong>{t('gender')}:</strong> {viewDonor.gender || '-'}</div>
              <div><strong>{t('sundaySchool')}:</strong> {viewDonor.isSundaySchoolMember ? t('yes') : t('no')}</div>
              <div><strong>{t('eligibilityStatus')}:</strong> {viewDonor.isEligible ? t('eligible') : t('notEligible')}</div>
              <div><strong>{t('previousDonationCount')}:</strong> {viewDonor.previousDonationCount}</div>
              <div><strong>{t('howHeard')}:</strong> {viewDonor.howHeardAboutUs || '-'}</div>
            </div>
            {viewDonor.donations?.length > 0 && (
              <>
                <h4 style={{ marginTop: 20 }}>{t('donationHistory')}</h4>
                <table>
                  <thead><tr><th>{t('lastDonationDate')}</th><th>{t('hospitalName')}</th><th>{t('notes')}</th></tr></thead>
                  <tbody>
                    {viewDonor.donations.map((dn) => (
                      <tr key={dn.id}>
                        <td>{new Date(dn.donationDate).toLocaleDateString()}</td>
                        <td>{dn.hospitalName || '-'}</td>
                        <td>{dn.notes || '-'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
