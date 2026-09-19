import { useEffect, useRef, useState } from 'react'
import type { ApiPage } from '../api'
import { errorText } from './errors'

export type PagingModel = { page: number; pageSize: number }

export type PagedQuery<T> = {
  rows: T[]
  total: number
  loading: boolean
  error: string
  paging: PagingModel
  setPaging: (model: PagingModel) => void
  /** Asks for the current page again, after something changed on the server. */
  reload: () => void
}

/**
 * The list behind a screen. It fetches a page whenever the filters or the page change, after a short pause so that typing
 * does not fire a request per key, ignores a slow answer that arrives after a newer one, and goes back to the first page
 * when the filters change. <c>filterKey</c> stands for the filters: any string that differs when they do.
 */
export function usePagedQuery<T>(
  load: (limit: number, offset: number) => Promise<ApiPage<T>>,
  filterKey: string,
  pageSize = 25,
  debounceMs = 250,
): PagedQuery<T> {
  const [paging, setPagingState] = useState<PagingModel>({ page: 0, pageSize })
  const [state, setState] = useState({ rows: [] as T[], total: 0, loading: true, error: '' })
  const [reloadKey, setReloadKey] = useState(0)
  const [seenFilterKey, setSeenFilterKey] = useState(filterKey)
  const started = useRef(false)

  if (filterKey !== seenFilterKey) {
    setSeenFilterKey(filterKey)
    setPagingState((current) => ({ ...current, page: 0 }))
  }

  useEffect(() => {
    let ignore = false
    // the first fetch does not wait; later ones (typing in a filter) do
    const wait = started.current ? debounceMs : 0
    started.current = true
    const handle = window.setTimeout(() => {
      setState((current) => ({ ...current, loading: true }))
      load(paging.pageSize, paging.page * paging.pageSize)
        .then((page) => {
          if (!ignore) setState({ rows: page.items, total: page.total, loading: false, error: '' })
        })
        .catch((reason: unknown) => {
          if (!ignore) setState((current) => ({ ...current, loading: false, error: errorText(reason) }))
        })
    }, wait)
    return () => {
      ignore = true
      window.clearTimeout(handle)
    }
    // `load` is a new function on every render; what decides a new fetch is the filters, the page and reload()
    // oxlint-disable-next-line react-hooks/exhaustive-deps
  }, [filterKey, paging.page, paging.pageSize, reloadKey])

  return {
    ...state,
    paging,
    setPaging: setPagingState,
    reload: () => setReloadKey((key) => key + 1),
  }
}
