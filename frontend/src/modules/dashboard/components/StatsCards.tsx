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
  subtitle,
}: {
  icon: ReactNode;
  iconBg: string;
  iconColor: string;
  title: string;
  value: number | string;
  subtitle?: string;
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
        <div style={{ minWidth: 0 }}>
          <div
            style={{
              fontSize: 12,
              color: 'var(--color-text-secondary)',
              marginBottom: 2,
              whiteSpace: 'nowrap',
              overflow: 'hidden',
              textOverflow: 'ellipsis',
            }}
          >
            {title}
          </div>
          <div
            style={{
              fontSize: 'clamp(22px, 4vw, 32px)',
              fontWeight: 700,
              lineHeight: 1.1,
              color: 'var(--color-text)',
              letterSpacing: '-0.5px',
            }}
          >
            {value}
          </div>
          {subtitle && (
            <div
              style={{
                fontSize: 12,
                color: 'var(--color-text-secondary)',
                marginTop: 4,
              }}
            >
              {subtitle}
            </div>
          )}
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
  const activeRules = endpoints.reduce(
    (sum, e) => sum + (e.rules?.filter((r) => r.isActive).length ?? 0),
    0,
  );
  const totalRequests = logs.length;
  const matchedRequests = logs.filter((l) => l.isMatched).length;
  const matchRate =
    totalRequests > 0 ? Math.round((matchedRequests / totalRequests) * 100) : 0;

  return (
    <Row gutter={[16, 16]}>
      <Col xs={12} lg={6}>
        <StatCard
          icon={<ApiOutlined />}
          iconBg="var(--stats-blue-bg)"
          iconColor="var(--stats-blue-icon)"
          title={t('dashboard.totalEndpoints')}
          value={totalEndpoints}
          subtitle={`${activeEndpoints} ${t('common.active').toLowerCase()}`}
        />
      </Col>
      <Col xs={12} lg={6}>
        <StatCard
          icon={<CheckCircleOutlined />}
          iconBg="var(--stats-green-bg)"
          iconColor="var(--stats-green-icon)"
          title={t('dashboard.activeEndpoints')}
          value={activeEndpoints}
          subtitle={
            totalEndpoints > 0
              ? `${Math.round((activeEndpoints / totalEndpoints) * 100)}%`
              : undefined
          }
        />
      </Col>
      <Col xs={12} lg={6}>
        <StatCard
          icon={<BranchesOutlined />}
          iconBg="var(--stats-purple-bg)"
          iconColor="var(--stats-purple-icon)"
          title={t('dashboard.totalRules')}
          value={totalRules}
          subtitle={`${activeRules} ${t('common.active').toLowerCase()}`}
        />
      </Col>
      <Col xs={12} lg={6}>
        <StatCard
          icon={<ThunderboltOutlined />}
          iconBg="var(--stats-orange-bg)"
          iconColor="var(--stats-orange-icon)"
          title={t('dashboard.totalRequests')}
          value={totalRequests}
          subtitle={
            totalRequests > 0
              ? `${t('dashboard.matchedRate')} ${matchRate}%`
              : undefined
          }
        />
      </Col>
    </Row>
  );
}
