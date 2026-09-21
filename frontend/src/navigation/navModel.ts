import type { ReactNode } from 'react'

export type NavEntry = { id: string; label: string }

/** A group with `items` folds open in the accordion; a group without them is itself the destination. */
export type NavGroup = { id: string; label: string; icon: ReactNode; badge?: number; items?: NavEntry[] }

/** The group that holds the view, if the menu has one for it. */
export function findGroupId(groups: NavGroup[], viewId: string): string | undefined {
  return groups.find((group) => (group.items ? group.items.some((item) => item.id === viewId) : group.id === viewId))?.id
}

/** What the menu calls the view: the item's label, or the group's own when the group is the destination. */
export function findLabel(groups: NavGroup[], viewId: string): string | undefined {
  for (const group of groups) {
    if (!group.items) {
      if (group.id === viewId) return group.label
      continue
    }
    const item = group.items.find((entry) => entry.id === viewId)
    if (item) return item.label
  }
  return undefined
}
