import { Layout, Menu, Typography, Button, Flex } from 'antd';
import {
  DashboardOutlined,
  ApiOutlined,
  FileTextOutlined,
  SwapOutlined,
  SunOutlined,
  MoonOutlined,
} from '@ant-design/icons';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import LanguageSwitcher from '../components/LanguageSwitcher';
import { useTheme } from '../contexts/ThemeContext';

const { Sider, Header, Content } = Layout;

export default function AppLayout() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const { mode, toggle } = useTheme();

  const menuItems = [
    { key: '/', icon: <DashboardOutlined />, label: t('menu.dashboard') },
    { key: '/endpoints', icon: <ApiOutlined />, label: t('menu.endpoints') },
    { key: '/logs', icon: <FileTextOutlined />, label: t('menu.logs') },
    { key: '/import-export', icon: <SwapOutlined />, label: t('menu.importExport') },
  ];

  const selectedKey = menuItems
    .filter((item) => location.pathname.startsWith(item.key) && item.key !== '/')
    .sort((a, b) => b.key.length - a.key.length)[0]?.key || '/';

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider
        width={240}
        theme="light"
        className="apple-sidebar"
        style={{
          position: 'fixed',
          left: 0,
          top: 0,
          bottom: 0,
          zIndex: 100,
          overflow: 'auto',
        }}
      >
        <div
          style={{
            height: 72,
            display: 'flex',
            alignItems: 'center',
            padding: '0 24px',
          }}
        >
          <Typography.Title
            level={4}
            style={{
              margin: 0,
              fontSize: 20,
              fontWeight: 700,
              letterSpacing: '-0.3px',
              color: 'var(--color-text)',
            }}
          >
            Mock Server
          </Typography.Title>
        </div>
        <Menu
          theme="light"
          mode="inline"
          selectedKeys={[selectedKey]}
          items={menuItems}
          onClick={({ key }) => navigate(key)}
          style={{ border: 'none', padding: '0 4px' }}
        />
      </Sider>
      <Layout style={{ marginLeft: 240 }}>
        <Header
          className="apple-header"
          style={{
            padding: '0 32px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'flex-end',
            height: 56,
            lineHeight: '56px',
            position: 'sticky',
            top: 0,
            zIndex: 99,
          }}
        >
          <Flex align="center" gap={4}>
            <Button
              type="text"
              icon={mode === 'dark' ? <SunOutlined /> : <MoonOutlined />}
              onClick={toggle}
              style={{ color: 'var(--color-text-secondary)' }}
            />
            <LanguageSwitcher />
          </Flex>
        </Header>
        <Content
          style={{
            padding: 32,
            maxWidth: 1200,
            width: '100%',
            margin: '0 auto',
          }}
        >
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  );
}
