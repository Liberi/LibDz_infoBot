using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDz_infoBot
{
    internal class UserValues
    {//глобальные переменные, которые используются для разных действий во всем коде
        public bool IsBlocked {  get; set; }  //блокирован ли пользователь, нужно для упрощения обращений к бд
        public string GroupName { get; set; } //текущая группа пользователя
        public Dictionary<string, bool> PressingButtons { get; set; } //действия ожидающие ввода
        public string KeyMenu { get; set; } //сохраняет название меню кнопок в котором мы сейчас находимся (для кнопки назад)
        public int NewsNumber { get; set; } //текущий номер новости для пролистывания последних
        public int MessageEditId { get; set;} //id для редактирования различных сообщений
        public object[] TablePicture { get; set; } //для сохранения размеров и типа картинки для дальнейшего их генерирования 
        public int DzInfoEditId {  get; set; } //для редактирования сообщений при редактировании Дз 
        public int MessageNewsId { get; set; } //id сообщения отображения новостей которое будем редактировать для пролистывания последних
        public int DzInfoAddId { get; set; } //для редактирования сообщений при добавлении Дз 
        public Dictionary<int, object[]> NameImgMessage { get; set;} //сохраняет id сообщения под которым была кнопка "В виде картинки" для правильного вывода картинок
        public int MessageProfileId { get; set; } //для редактирования сообщений при редактировании профиля
        public string ConfirmValue { get; set; } //для понимания какое именно действие мы подтверждаем нажатием на кнопку "Подтвердить"
        public int AddImgNewsId {  get; set; } //добавление картинки к новости по ее номеру 
        public DateTime ChangeGroupTime { get; set; } //изменять группу для всех пользователей кроме 1типа админа можно раз в 10 мин, тут сохранено когда последний раз мы меняли
        public int CullsInfoEditId { get; set; } //для редактирования сообщений при редактировании звонков
        public DateTime LastTimeActive { get; set; } //для очистки всех перечисленных переменных, что-бы не загружать массив, если пользователь не был активен более 24ч
    }
}
