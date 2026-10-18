export type TileColor = 'amber' | 'blue' | 'violet' | 'cyan' | 'lime' | 'orange' | 'pink' | 'stone'

export const tileColorClasses: Record<TileColor, string> = {
  amber: 'bg-amber-100 text-amber-700 dark:bg-amber-400/16 dark:text-amber-300',
  blue: 'bg-blue-100 text-blue-700 dark:bg-blue-400/18 dark:text-blue-300',
  violet: 'bg-violet-100 text-violet-700 dark:bg-violet-400/20 dark:text-violet-300',
  cyan: 'bg-cyan-100 text-cyan-700 dark:bg-cyan-400/16 dark:text-cyan-300',
  lime: 'bg-lime-100 text-lime-700 dark:bg-lime-400/16 dark:text-lime-300',
  orange: 'bg-orange-100 text-orange-700 dark:bg-orange-400/18 dark:text-orange-300',
  pink: 'bg-pink-100 text-pink-700 dark:bg-pink-400/18 dark:text-pink-300',
  stone: 'bg-stone-100 text-stone-600 dark:bg-stone-400/16 dark:text-stone-300',
}
