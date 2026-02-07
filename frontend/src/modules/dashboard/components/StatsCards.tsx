import { Row, Col, Card } from 'antd';
import {
  ApiOutlined,
  CheckCircleOutlined,
  BranchesOutlined,
  ThunderboltOutlined,
} from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import type { MockEndpoint, MockRequestLog } from '@/shared/types';
import type { ReactNode } from 'react';

interface StatsCardsProps {
  endpoints: MockEndpoint[];
  logs: MockRequestLog[];
}

function StatCard({
  icon,
  iconBg,
  iconColor,
  title,
  value,
}: {
  icon: ReactNode;
  iconBg: string;
  iconColor: string;
  title: string;
  value: number;
}) {
  return (
    <Card style={{ padding: 4 }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 16 }}>
        <div
          style={{
            width: 48,
            height: 48,
            borderRadius: 14,
            background: iconBg,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            fontSize: 22,
            color: iconColor,
            flexShrink: 0,
          }}
        >
          {icon}
        </div>
        <div>
          <div
            style={{
              fontSize: 13,
              color: 'var(--color-text-secondary)',
              marginBottom: 2,
            }}
          >
            {title}
          </div>
          <div
            style={{
              fontSize: 32,
              fontWeight: 700,
              lineHeight: 1.1,
              color: 'var(--color-text)',
              letterSpacing: '-0.5px',
            }}
          >
            {value}
          </div>
        </div>
      </div>
    </Card>
  );
}

export default function StatsCards({ endpoints, logs }: StatsCardsProps) {
  const { t } = useTranslation();

  const totalEndpoints = endpoints.length;
  const activeEndpoints = endpoints.filter((e) => e.isActive).length;
  const totalRules = endpoints.reduce((sum, e) => sum + (e.rules?.length ?? 0), 0);
  const totalRequests = logs.length;

  return (
    <Row gutter={[16, 16]}>
      <Col xs={12} sm={6}>
        <StatCard
          icon={<ApiOutlined />}
          iconBg="var(--stats-blue-bg)"
          iconColor="var(--stats-blue-icon)"
          title={t('dashboard.totalEndpoints')}
          value={totalEndpoints}
        />
      </Col>
      <Col xs={12} sm={6}>
        <StatCard
          icon={<CheckCircleOutlined />}
          iconBg="var(--stats-green-bg)"
          iconColor="var(--stats-green-icon)"
          title={t('dashboard.activeEndpoints')}
          value={activeEndpoints}
        />
      </Col>
      <Col xs={12} sm={6}>
        <StatCard
          icon={<BranchesOutlined />}
          iconBg="var(--stats-purple-bg)"
          iconColor="var(--stats-purple-icon)"
          title={t('dashboard.totalRules')}
          value={totalRules}
        />
      </Col>
      <Col xs={12} sm={6}>
        <StatCard
          icon={<ThunderboltOutlined />}
          iconBg="var(--stats-orange-bg)"
          iconColor="var(--stats-orange-icon)"
          title={t('dashboard.totalRequests')}
          value={totalRequests}
        />
      </Col>
    </Row>
  );
}
