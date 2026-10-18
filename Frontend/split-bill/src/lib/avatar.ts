// 中文名字取最後一個字，否則取第一個字母並轉成大寫
export const getInitial = (name: string): string => {
  const characters = Array.from(name.trim())
  const hanCharacters = characters.filter((character) => /\p{Script=Han}/u.test(character))

  if (hanCharacters.length > 0) {
    return hanCharacters[hanCharacters.length - 1] ?? ''
  }

  return (characters[0] ?? '').toUpperCase()
}
