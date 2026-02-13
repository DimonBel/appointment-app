interface LoadingSkeletonProps {
  className?: string;
}

export const CardSkeleton = ({ className = '' }: LoadingSkeletonProps) => (
  <div className={`bg-white rounded-xl border border-gray-200 overflow-hidden ${className}`}>
    <div className="h-32 bg-gradient-to-r from-blue-500 to-blue-600 animate-pulse" />
    <div className="p-6 space-y-4">
      <div className="h-4 bg-gray-200 rounded animate-pulse w-3/4" />
      <div className="h-4 bg-gray-200 rounded animate-pulse w-1/2" />
      <div className="h-4 bg-gray-200 rounded animate-pulse w-2/3" />
      <div className="h-10 bg-gray-200 rounded-lg animate-pulse mt-4" />
    </div>
  </div>
);

export const TableRowSkeleton = () => (
  <tr className="animate-pulse">
    <td className="px-6 py-4">
      <div className="h-4 bg-gray-200 rounded w-24 mb-2" />
      <div className="h-3 bg-gray-200 rounded w-16" />
    </td>
    <td className="px-6 py-4">
      <div className="h-4 bg-gray-200 rounded w-32" />
    </td>
    <td className="px-6 py-4">
      <div className="h-4 bg-gray-200 rounded w-16" />
    </td>
    <td className="px-6 py-4">
      <div className="h-6 bg-gray-200 rounded-full w-20" />
    </td>
    <td className="px-6 py-4">
      <div className="h-8 bg-gray-200 rounded w-24" />
    </td>
  </tr>
);

export const StatCardSkeleton = () => (
  <div className="bg-white rounded-xl shadow-sm p-6 border border-gray-200">
    <div className="flex items-center justify-between">
      <div className="flex-1">
        <div className="h-4 bg-gray-200 rounded w-20 mb-2 animate-pulse" />
        <div className="h-8 bg-gray-200 rounded w-16 animate-pulse" />
      </div>
      <div className="w-12 h-12 bg-gray-200 rounded-lg animate-pulse" />
    </div>
  </div>
);

export const ButtonSkeleton = ({ width = '100px' }: { width?: string }) => (
  <div className={`h-10 bg-gray-200 rounded-lg animate-pulse`} style={{ width }} />
);

export default function LoadingSkeleton({ className = '' }: LoadingSkeletonProps) {
  return <div className={`animate-pulse bg-gray-200 rounded ${className}`} />;
}