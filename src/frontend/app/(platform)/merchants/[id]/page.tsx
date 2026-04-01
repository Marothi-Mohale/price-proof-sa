import { MerchantHistoryScreen } from "@/components/merchants/merchant-history-screen";

export default function MerchantHistoryPage({ params }: { params: { id: string } }) {
  return <MerchantHistoryScreen merchantId={params.id} />;
}
