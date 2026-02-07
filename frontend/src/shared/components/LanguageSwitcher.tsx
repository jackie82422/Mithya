import { Button, Dropdown } from 'antd';
import { GlobalOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';

const languages = [
  { key: 'zh-TW', label: '繁體中文' },
  { key: 'en', label: 'English' },
];

export default function LanguageSwitcher() {
  const { i18n } = useTranslation();

  return (
    <Dropdown
      menu={{
        items: languages.map((lang) => ({
          key: lang.key,
          label: lang.label,
        })),
        selectedKeys: [i18n.language],
        onClick: ({ key }) => {
          i18n.changeLanguage(key);
          localStorage.setItem('lang', key);
        },
      }}
    >
      <Button type="text" icon={<GlobalOutlined />} />
    </Dropdown>
  );
}
