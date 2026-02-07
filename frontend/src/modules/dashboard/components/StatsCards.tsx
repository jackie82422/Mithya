import { Row, Col, Card, Statistic } from 'antd';
import {
  ApiOutlined,
  CheckCircleOutlined,
  BranchesOutlined,
  ThunderboltOutlined,
} from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import type { MockEndpoint, MockRequestLog } from '@/shared/types';

interface StatsCardsProps {
  endpoints: MockEndpoint[];
  logs: MockRequestLog[];
}

export default function StatsCards({ endpoints, logs }: StatsCardsProps) {
  const { t } = useTranslation();

  const totalEndpoints = endpoints.length;
  const activeEndpoints = endpoints.filter((e) => e.isActive).length;
  const totalRules = endpoints.reduce((sum, e) => sum + (e.rules?.length ?? 0), 0);
  const totalRequests = logs.length;

  return (
    <Row gutter={16}>
      <Col xs={12} sm={6}>
        <Card>
          <Statistic
            title={t('dashboard.totalEndpoints')}
            value={totalEndpoints}
            prefix={<ApiOutlined />}
          />
        </Card>
      </Col>
      <Col xs={12} sm={6}>
        <Card>
          <Statistic
            title={t('dashboard.activeEndpoints')}
            value={activeEndpoints}
            prefix={<CheckCircleOutlined />}
            valueStyle={{ color: '#52c41a' }}
          />
        </Card>
      </Col>
      <Col xs={12} sm={6}>
        <Card>
          <Statistic
            title={t('dashboard.totalRules')}
            value={totalRules}
            prefix={<BranchesOutlined />}
          />
        </Card>
      </Col>
      <Col xs={12} sm={6}>
        <Card>
          <Statistic
            title={t('dashboard.totalRequests')}
            value={totalRequests}
            prefix={<ThunderboltOutlined />}
          />
        </Card>
      </Col>
    </Row>
  );
}
