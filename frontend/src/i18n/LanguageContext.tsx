import { createContext, useContext, useState, useCallback, type ReactNode } from 'react';
import { translations, type Language, type TranslationKey } from './translations';

interface LanguageContextType {
  lang: Language;
  setLang: (lang: Language) => void;
  t: (key: TranslationKey) => string;
}

const LanguageContext = createContext<LanguageContextType | null>(null);

export function LanguageProvider({ children }: { children: ReactNode }) {
  const [lang, setLang] = useState<Language>(() => {
    return (localStorage.getItem('lang') as Language) || 'en';
  });

  const handleSetLang = useCallback((l: Language) => {
    setLang(l);
    localStorage.setItem('lang', l);
  }, []);

  const t = useCallback(
    (key: TranslationKey) => translations[lang][key] || translations.en[key] || key,
    [lang]
  );

  return (
    <LanguageContext.Provider value={{ lang, setLang: handleSetLang, t }}>
      {children}
    </LanguageContext.Provider>
  );
}

export function useLanguage() {
  const ctx = useContext(LanguageContext);
  if (!ctx) throw new Error('useLanguage must be used within LanguageProvider');
  return ctx;
}
