import { useState } from 'react'
import { TextField, type TextFieldProps } from '@mui/material'

const parse = (text: string): number => (text.trim() === '' ? 0 : Number(text.trim().replace(',', '.')))

type AmountFieldProps = { value: number; onChange: (value: number) => void } & Omit<TextFieldProps, 'value' | 'onChange' | 'type'>

/**
 * A number the user types: it takes a comma or a dot, may be empty (zero) while it is being typed, and keeps what was typed
 * as long as it still means the same number, so "2." or "0,5" are not rewritten under the cursor.
 */
export function AmountField({ value, onChange, slotProps, ...rest }: AmountFieldProps) {
  const [text, setText] = useState(value === 0 ? '' : String(value))
  const [seen, setSeen] = useState(value)

  // the number was changed from outside (a quick-fill button, a reset): show it, unless what is typed already means it
  if (value !== seen) {
    setSeen(value)
    if (parse(text) !== value) setText(value === 0 ? '' : String(value))
  }

  return (
    <TextField
      {...rest}
      value={text}
      placeholder={rest.placeholder ?? '0'}
      onChange={(event) => {
        setText(event.target.value)
        const next = parse(event.target.value)
        if (!Number.isNaN(next)) onChange(next)
      }}
      slotProps={{ ...slotProps, htmlInput: { inputMode: 'decimal', ...(slotProps?.htmlInput as object | undefined) } }}
    />
  )
}
