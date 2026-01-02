import { useState, useMemo, useEffect } from 'react';

interface UsePaginationProps<T> {
  items: T[];
  initialPageSize?: number;
}

interface UsePaginationResult<T> {
  currentPage: number;
  pageSize: number;
  paginatedItems: T[];
  totalPages: number;
  totalItems: number;
  setPage: (page: number) => void;
  setPageSize: (size: number) => void;
}

export function usePagination<T>({
  items,
  initialPageSize = 10,
}: UsePaginationProps<T>): UsePaginationResult<T> {
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(initialPageSize);

  const totalItems = items.length;
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize));

  // Reset to page 1 if current page exceeds total pages
  useEffect(() => {
    if (currentPage > totalPages) {
      setCurrentPage(totalPages);
    }
  }, [currentPage, totalPages]);

  const paginatedItems = useMemo(() => {
    const startIndex = (currentPage - 1) * pageSize;
    const endIndex = startIndex + pageSize;
    return items.slice(startIndex, endIndex);
  }, [items, currentPage, pageSize]);

  const handlePageChange = (page: number) => {
    if (page >= 1 && page <= totalPages) {
      setCurrentPage(page);
    }
  };

  const handlePageSizeChange = (size: number) => {
    setPageSize(size);
    setCurrentPage(1); // Reset to first page when changing page size
  };

  return {
    currentPage,
    pageSize,
    paginatedItems,
    totalPages,
    totalItems,
    setPage: handlePageChange,
    setPageSize: handlePageSizeChange,
  };
}
