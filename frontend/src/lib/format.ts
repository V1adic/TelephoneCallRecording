export interface PeriodRange {
  from: string
  to: string
}

const dateTimeFormatter = new Intl.DateTimeFormat('ru-RU', {
  dateStyle: 'medium',
  timeStyle: 'short'
})

const currencyFormatter = new Intl.NumberFormat('ru-RU', {
  style: 'currency',
  currency: 'RUB',
  maximumFractionDigits: 2
})

export function createDefaultPeriod(days = 30): PeriodRange {
  const to = new Date()
  const from = new Date()
  from.setDate(to.getDate() - days)

  return {
    from: toInputDate(from),
    to: toInputDate(to)
  }
}

export function toInputDate(value: Date) {
  const year = value.getFullYear()
  const month = `${value.getMonth() + 1}`.padStart(2, '0')
  const day = `${value.getDate()}`.padStart(2, '0')
  return `${year}-${month}-${day}`
}

export function periodToUtc(range: PeriodRange) {
  const from = new Date(`${range.from}T00:00:00`)
  const to = new Date(`${range.to}T00:00:00`)
  to.setDate(to.getDate() + 1)

  return {
    fromUtc: from.toISOString(),
    toUtc: to.toISOString()
  }
}

export function formatDateTime(value: string) {
  return dateTimeFormatter.format(new Date(value))
}

export function formatCurrency(value: number) {
  return currencyFormatter.format(value)
}

export function formatTimeOfDay(value: string) {
  return value.toLowerCase() === 'day' ? 'День' : 'Ночь'
}

export function formatRole(value: string) {
  return value === 'Admin' ? 'Администратор' : 'Клиент'
}

export function formatAccessState(value: string) {
  switch (value) {
    case 'revoked':
      return 'Доступ отозван'
    case 'locked':
      return 'Временная блокировка'
    default:
      return 'Активен'
  }
}
