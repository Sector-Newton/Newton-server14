cmd-addmedia-desc = Добавить игрока в медиа список сервера.
cmd-addmedia-help = Использование: addmedia <username или  User ID>
cmd-addmedia-existing = { $username } уже находится в медиа списке!
cmd-addmedia-added = { $username } добавлен в медиа список
cmd-addmedia-not-found = Не удалось найти игрока '{ $username }'
cmd-addmedia-arg-player = [player]

cmd-removemedia-desc = Удалить игрока с медиа списка сервера.
cmd-removemedia-help = Использование: removemedia <username или  User ID>
cmd-removemedia-existing = { $username } не находится в медиа списке!
cmd-removemedia-removed = { $username } удалён из медиа списка
cmd-removemedia-not-found = Не удалось найти игрока '{ $username }'
cmd-removemedia-arg-player = [player]

cmd-medialist-line-seeall = C-Key: { $username } ({ $guid })
cmd-medialist-line = C-Key: { $username }
cmd-medialist-notfound = Список пуст

cmd-forcedeadmin-succeed = Player { $username } deadmined
cmd-forcedeadmin-non-admin = Игрок { $username } не является администратором
cmd-forcedeadmin-in-deadmin = Игрок { $username } уже в deadmin
cmd-forcedeadmin-has-flags = Вы не можете убрать права администратора у этого игрока
cmd-forcedeadmin-arg-user = <user name>
cmd-forcedeadmin-error-args = Должен быть ровно 1 аргумент
cmd-forcedeadmin-desc = Насильно убирает права администратора у игрока
cmd-forcedeadmin-help = Использование: { $command } <user name>