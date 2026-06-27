import axios from 'axios';

const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

const api = axios.create({ baseURL: API_BASE });

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

api.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err.response?.status === 401) {
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      if (window.location.pathname !== '/login') window.location.href = '/login';
    }
    return Promise.reject(err);
  }
);

export default api;

export interface LoginResponse {
  token: string;
  username: string;
  fullName: string;
  role: string;
  expiresAt: string;
}

export interface DashboardSummary {
  totalDonors: number;
  eligibleDonors: number;
  nonEligibleDonors: number;
}

export interface BloodTypeDistribution {
  bloodType: string;
  count: number;
}

export interface RecentDonor {
  id: string;
  fullName: string;
  christianName: string;
  phone: string;
  bloodType: string;
  createdAt: string;
}

export interface Notification {
  id: string;
  titleEn: string;
  titleAm?: string;
  messageEn: string;
  messageAm?: string;
  notificationType: string;
  eventDate?: string;
}

export interface DonorListItem {
  id: string;
  fullName: string;
  christianName: string;
  phone: string;
  bloodType: string;
  lastDonationDate?: string;
  isEligible: boolean;
  isSundaySchoolMember: boolean;
  createdAt: string;
}

export interface DonorDetail extends DonorListItem {
  email?: string;
  gender?: string;
  isFirstTimeDonor: boolean;
  previousDonationCount: number;
  howHeardAboutUs?: string;
  eligibilityNotes?: string;
  donations: DonationRecord[];
}

export interface DonationRecord {
  id: string;
  donationDate: string;
  hospitalName?: string;
  verifiedBy?: string;
  notes?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface SmsBalance {
  balanceEtb: number;
  estimatedMessages: number;
  costPerMessage: number;
}

export interface SmsLog {
  id: string;
  recipientType: string;
  recipientCount: number;
  messageContent: string;
  status: string;
  deliveryStatus?: string;
  errorMessage?: string;
  sentAt: string;
}

export interface Hospital {
  id: string;
  name: string;
  contactPerson?: string;
  phone?: string;
  email?: string;
  notes?: string;
  isActive: boolean;
}

export interface ReportMetadata {
  id: string;
  reportType: string;
  fileName: string;
  fileFormat: string;
  dateFrom?: string;
  dateTo?: string;
  recordCount: number;
  generatedAt: string;
}

export const BLOOD_TYPES = ['A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-', 'Unknown'];
