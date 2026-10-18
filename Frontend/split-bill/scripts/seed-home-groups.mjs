// 建立首頁群組清單的測試資料：七個群組，涵蓋首頁每一種顯示狀態
// 用法（在 Frontend/split-bill 底下）：node scripts/seed-home-groups.mjs <token>
// 只呼叫本機後端；要換位址時設定環境變數 API_URL

const token = process.argv[2]
const apiUrl = process.env.API_URL ?? 'http://localhost:5094/api'

if (!token) {
  console.error('請在指令後面貼上 token：node scripts/seed-home-groups.mjs <token>')
  process.exit(1)
}

const callApi = async (method, path, body) => {
  const options = {
    method,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
  }
  if (body !== undefined) {
    options.body = JSON.stringify(body)
  }

  const response = await fetch(`${apiUrl}${path}`, options)
  const text = await response.text()

  if (!response.ok) {
    throw new Error(`${method} ${path} 失敗（${response.status}）：${text}`)
  }

  return text ? JSON.parse(text) : null
}

// 後端只接受不晚於 UTC 當下日期次日的消費日期，取 UTC 的今天最保險
const today = new Date().toISOString().slice(0, 10)

// 付款人與分攤都以顯示名稱表示，「我」代表呼叫者自己的成員位置
const scenarios = [
  {
    expected: '還有帳：你會收回 JPY 8,200',
    group: { name: '東京旅行', icon: 'Travel', baseCurrency: 'JPY' },
    members: ['小安', '阿哲', 'Mia'],
    expenses: [
      {
        name: '燒肉晚餐',
        category: 'Food',
        amount: 12000,
        payer: '我',
        shares: { 我: 3000, 小安: 3000, 阿哲: 3000, Mia: 3000 },
      },
      {
        name: '地鐵一日券',
        category: 'Transport',
        amount: 3200,
        payer: '小安',
        shares: { 我: 800, 小安: 800, 阿哲: 800, Mia: 800 },
      },
    ],
  },
  {
    expected: '還有帳：你要付 TWD 1,250',
    group: { name: '室友帳', icon: 'Home', baseCurrency: 'TWD' },
    members: ['小明', '阿華'],
    expenses: [
      {
        name: '九月水電',
        category: 'Other',
        amount: 3750,
        payer: '小明',
        shares: { 我: 1250, 小明: 1250, 阿華: 1250 },
      },
    ],
  },
  {
    expected: '還有帳：你會收回 USD 42.75',
    group: { name: '紐約出差', icon: 'Travel', baseCurrency: 'USD' },
    members: ['Amy'],
    expenses: [
      {
        name: '牛排晚餐',
        category: 'Food',
        amount: 85.5,
        payer: '我',
        shares: { 我: 42.75, Amy: 42.75 },
      },
    ],
  },
  {
    expected: '還有帳但個人淨額為 0：小安欠你 TWD 600、你欠 Mia TWD 600',
    group: { name: '週末火鍋', icon: 'Dining', baseCurrency: 'TWD' },
    members: ['小安', 'Mia'],
    expenses: [
      {
        name: '火鍋',
        category: 'Food',
        amount: 1200,
        payer: '我',
        shares: { 我: 600, 小安: 600 },
      },
      {
        name: '飲料',
        category: 'Food',
        amount: 1200,
        payer: 'Mia',
        shares: { 我: 600, Mia: 600 },
      },
    ],
  },
  {
    expected: '已結清，但不是全員結清：小明欠阿華 TWD 1,000',
    group: { name: '羽球團', icon: 'Sports', baseCurrency: 'TWD' },
    members: ['阿華', '小明'],
    expenses: [
      {
        name: '場地費',
        category: 'Entertainment',
        amount: 2000,
        payer: '阿華',
        shares: { 阿華: 1000, 小明: 1000 },
      },
    ],
  },
  {
    expected: '全員結清：沒有任何花費',
    group: { name: '生日派對', icon: 'Party', baseCurrency: 'TWD' },
    members: ['小安', '阿哲'],
    expenses: [],
  },
  {
    expected: '已結束：只出現在「已結束」的數量裡',
    group: { name: '墾丁三天兩夜', icon: 'Travel', baseCurrency: 'TWD' },
    members: ['阿哲'],
    expenses: [],
    closed: true,
  },
]

const existingGroups = await callApi('GET', '/group')
const existingNames = new Set(existingGroups.map((group) => group.name))
const duplicatedNames = scenarios
  .map((scenario) => scenario.group.name)
  .filter((name) => existingNames.has(name))

if (duplicatedNames.length > 0) {
  console.error(`已經有同名的群組：${duplicatedNames.join('、')}`)
  console.error('看起來之前跑過了；要重跑請先刪掉這些群組')
  process.exit(1)
}

for (const scenario of scenarios) {
  const group = await callApi('POST', '/group', { ...scenario.group, description: '' })

  const owners = await callApi('GET', `/group/${group.id}/members`)
  const myMember = owners.find((member) => member.isMe)
  const addedMembers = await callApi('POST', `/group/${group.id}/members`, {
    displayNames: scenario.members,
  })

  const memberIdByName = new Map([['我', myMember.id]])
  for (const member of addedMembers) {
    memberIdByName.set(member.displayName, member.id)
  }

  for (const expense of scenario.expenses) {
    await callApi('POST', `/group/${group.id}/expenses`, {
      name: expense.name,
      description: '',
      category: expense.category,
      currency: scenario.group.baseCurrency,
      amount: expense.amount,
      date: today,
      payerId: memberIdByName.get(expense.payer),
      shares: Object.entries(expense.shares).map(([name, amount]) => ({
        memberId: memberIdByName.get(name),
        amount,
      })),
    })
  }

  if (scenario.closed) {
    await callApi('PUT', `/group/${group.id}/closed`, { closed: true })
  }

  console.log(`✓ ${scenario.group.name}：${scenario.expected}`)
}

console.log('完成。首頁進行中的順序應為：週末火鍋、紐約出差、室友帳、東京旅行、生日派對、羽球團')
