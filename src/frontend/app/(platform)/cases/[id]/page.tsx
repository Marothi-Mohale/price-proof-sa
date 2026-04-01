import { CaseDetailScreen } from "@/components/cases/case-detail-screen";

export default function CaseDetailPage({ params }: { params: { id: string } }) {
  return <CaseDetailScreen caseId={params.id} />;
}
