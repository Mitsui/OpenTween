﻿// OpenTween - Client of Twitter
// Copyright (c) 2007-2011 kiri_feather (@kiri_feather) <kiri.feather@gmail.com>
//           (c) 2008-2011 Moz (@syo68k)
//           (c) 2008-2011 takeshik (@takeshik) <http://www.takeshik.org/>
//           (c) 2010-2011 anis774 (@anis774) <http://d.hatena.ne.jp/anis774/>
//           (c) 2010-2011 fantasticswallow (@f_swallow) <http://twitter.com/f_swallow>
//           (c) 2011      kim_upsilon (@kim_upsilon) <https://upsilo.net/~upsilon/>
// All rights reserved.
// 
// This file is part of OpenTween.
// 
// This program is free software; you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the Free
// Software Foundation; either version 3 of the License, or (at your option)
// any later version.
// 
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License
// for more details. 
// 
// You should have received a copy of the GNU General Public License along
// with this program. If not, see <http://www.gnu.org/licenses/>, or write to
// the Free Software Foundation, Inc., 51 Franklin Street - Fifth Floor,
// Boston, MA 02110-1301, USA.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Web;
using System.IO;
using System.Net;

namespace OpenTween
{
    public partial class UserInfoDialog : OTBaseForm
    {
        private TwitterDataModel.User _displayUser;
        public TwitterDataModel.User DisplayUser
        {
            get { return this._displayUser; }
            set
            {
                if (this._displayUser != value)
                {
                    this._displayUser = value;
                    this.OnDisplayUserChanged();
                }
            }
        }

        private new TweenMain Owner
        {
            get { return (TweenMain)base.Owner; }
        }

        private Twitter Twitter
        {
            get { return this.Owner.TwitterInstance; }
        }

        public UserInfoDialog()
        {
            InitializeComponent();

            // LabelScreenName のフォントを OTBaseForm.GlobalFont に変更
            this.LabelScreenName.Font = this.ReplaceToGlobalFont(this.LabelScreenName.Font);
        }

        protected virtual async void OnDisplayUserChanged()
        {
            if (this._displayUser == null)
                return;

            var user = this._displayUser;

            this.LabelId.Text = user.IdStr;
            this.LabelScreenName.Text = user.ScreenName;
            this.LabelName.Text = WebUtility.HtmlDecode(user.Name);
            this.LabelLocation.Text = user.Location ?? "";
            this.LabelCreatedAt.Text = MyCommon.DateTimeParse(user.CreatedAt).ToString();

            if (user.Protected)
                this.LabelIsProtected.Text = Properties.Resources.Yes;
            else
                this.LabelIsProtected.Text = Properties.Resources.No;

            if (user.Verified)
                this.LabelIsVerified.Text = Properties.Resources.Yes;
            else
                this.LabelIsVerified.Text = Properties.Resources.No;

            var followingUrl = "https://twitter.com/" + user.ScreenName + "/following";
            this.LinkLabelFollowing.Text = user.FriendsCount.ToString();
            this.LinkLabelFollowing.Tag = followingUrl;
            this.ToolTip1.SetToolTip(this.LinkLabelFollowing, followingUrl);

            var followersUrl = "https://twitter.com/" + user.ScreenName + "/followers";
            this.LinkLabelFollowers.Text = user.FollowersCount.ToString();
            this.LinkLabelFollowers.Tag = followersUrl;
            this.ToolTip1.SetToolTip(this.LinkLabelFollowers, followersUrl);

            var favoritesUrl = "https://twitter.com/" + user.ScreenName + "/favorites";
            this.LinkLabelFav.Text = user.FavouritesCount.ToString();
            this.LinkLabelFav.Tag = favoritesUrl;
            this.ToolTip1.SetToolTip(this.LinkLabelFav, favoritesUrl);

            var profileUrl = "https://twitter.com/" + user.ScreenName;
            this.LinkLabelTweet.Text = user.StatusesCount.ToString();
            this.LinkLabelTweet.Tag = profileUrl;
            this.ToolTip1.SetToolTip(this.LinkLabelTweet, profileUrl);

            if (this.Twitter.UserId == user.Id)
            {
                this.ButtonEdit.Enabled = true;
                this.ChangeIconToolStripMenuItem.Enabled = true;
                this.ButtonBlock.Enabled = false;
                this.ButtonReportSpam.Enabled = false;
                this.ButtonBlockDestroy.Enabled = false;
            }
            else
            {
                this.ButtonEdit.Enabled = false;
                this.ChangeIconToolStripMenuItem.Enabled = false;
                this.ButtonBlock.Enabled = true;
                this.ButtonReportSpam.Enabled = true;
                this.ButtonBlockDestroy.Enabled = true;
            }

            await Task.WhenAll(new[]
            {
                this.SetDescriptionAsync(user.Description),
                this.SetRecentStatusAsync(user.Status),
                this.SetLinkLabelWebAsync(user.Url),
                this.SetUserImageAsync(user.ProfileImageUrlHttps),
                this.LoadFriendshipAsync(user.ScreenName),
            });
        }

        private async Task SetDescriptionAsync(string descriptionText)
        {
            if (descriptionText != null)
            {
                var atlist = new List<string>();

                var html = WebUtility.HtmlEncode(descriptionText);
                html = await this.Twitter.CreateHtmlAnchorAsync(html, atlist, null);
                html = this.Owner.createDetailHtml(html);

                this.DescriptionBrowser.DocumentText = html;
            }
            else
            {
                this.DescriptionBrowser.DocumentText = "";
            }
        }

        private async Task SetUserImageAsync(string imageUri)
        {
            var oldImage = this.UserPicture.Image;
            if (oldImage != null)
            {
                this.UserPicture.Image = null;
                oldImage.Dispose();
            }

            using (var http = MyCommon.CreateHttpClient())
            {
                var imageStream = await http.GetStreamAsync(imageUri.Replace("_normal", "_bigger"));
                this.UserPicture.Image = await MemoryImage.CopyFromStreamAsync(imageStream);
            }
        }

        private async Task SetLinkLabelWebAsync(string uri)
        {
            if (uri != null)
            {
                var expandedUrl = await ShortUrl.Instance.ExpandUrlStrAsync(uri);

                this.LinkLabelWeb.Text = uri;
                this.LinkLabelWeb.Tag = expandedUrl;
                this.ToolTip1.SetToolTip(this.LinkLabelWeb, expandedUrl);
            }
            else
            {
                this.LinkLabelWeb.Text = "";
                this.LinkLabelWeb.Tag = null;
                this.ToolTip1.SetToolTip(this.LinkLabelWeb, null);
            }
        }

        private async Task SetRecentStatusAsync(TwitterDataModel.Status status)
        {
            var atlist = new List<string>();

            if (status != null)
            {
                var html = await this.Twitter.CreateHtmlAnchorAsync(status.Text, atlist, status.Entities, null);
                html = this.Owner.createDetailHtml(html +
                    " Posted at " + MyCommon.DateTimeParse(status.CreatedAt) +
                    " via " + status.Source);

                this.RecentPostBrowser.DocumentText = html;
            }
            else
            {
                this.RecentPostBrowser.DocumentText = Properties.Resources.ShowUserInfo2;
            }
        }

        private async Task LoadFriendshipAsync(string screenName)
        {
            this.LabelIsFollowing.Text = "";
            this.LabelIsFollowed.Text = "";
            this.ButtonFollow.Enabled = false;
            this.ButtonUnFollow.Enabled = false;

            if (this.Twitter.Username == screenName)
                return;

            var friendship = await Task.Run(() =>
            {
                var IsFollowing = false;
                var IsFollowedBy = false;

                var ret = this.Twitter.GetFriendshipInfo(screenName, ref IsFollowing, ref IsFollowedBy);
                if (!string.IsNullOrEmpty(ret))
                    return null;

                return new { IsFollowing, IsFollowedBy };
            });

            if (friendship == null)
            {
                LabelIsFollowed.Text = Properties.Resources.GetFriendshipInfo6;
                LabelIsFollowing.Text = Properties.Resources.GetFriendshipInfo6;
                return;
            }

            this.LabelIsFollowing.Text = friendship.IsFollowing
                ? Properties.Resources.GetFriendshipInfo1
                : Properties.Resources.GetFriendshipInfo2;

            this.LabelIsFollowed.Text = friendship.IsFollowedBy
                ? Properties.Resources.GetFriendshipInfo3
                : Properties.Resources.GetFriendshipInfo4;

            this.ButtonFollow.Enabled = !friendship.IsFollowing;
            this.ButtonUnFollow.Enabled = friendship.IsFollowing;
        }

        private void ShowUserInfo_Load(object sender, EventArgs e)
        {
            this.TextBoxName.Location = this.LabelName.Location;
            this.TextBoxName.Height = this.LabelName.Height;
            this.TextBoxName.Width = this.LabelName.Width;
            this.TextBoxName.BackColor = this.Owner.InputBackColor;
            this.TextBoxName.MaxLength = 20;

            this.TextBoxLocation.Location = this.LabelLocation.Location;
            this.TextBoxLocation.Height = this.LabelLocation.Height;
            this.TextBoxLocation.Width = this.LabelLocation.Width;
            this.TextBoxLocation.BackColor = this.Owner.InputBackColor;
            this.TextBoxLocation.MaxLength = 30;

            this.TextBoxWeb.Location = this.LinkLabelWeb.Location;
            this.TextBoxWeb.Height = this.LinkLabelWeb.Height;
            this.TextBoxWeb.Width = this.LinkLabelWeb.Width;
            this.TextBoxWeb.BackColor = this.Owner.InputBackColor;
            this.TextBoxWeb.MaxLength = 100;

            this.TextBoxDescription.Location = this.DescriptionBrowser.Location;
            this.TextBoxDescription.Height = this.DescriptionBrowser.Height;
            this.TextBoxDescription.Width = this.DescriptionBrowser.Width;
            this.TextBoxDescription.BackColor = this.Owner.InputBackColor;
            this.TextBoxDescription.MaxLength = 160;
            this.TextBoxDescription.Multiline = true;
            this.TextBoxDescription.ScrollBars = ScrollBars.Vertical;
        }

        private async void LinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var linkLabel = (LinkLabel)sender;

            var linkUrl = (string)linkLabel.Tag;
            if (linkUrl == null)
                return;

            await this.Owner.OpenUriAsync(linkUrl);
        }

        private void ButtonFollow_Click(object sender, EventArgs e)
        {
            string ret = this.Twitter.PostFollowCommand(this._displayUser.ScreenName);
            if (!string.IsNullOrEmpty(ret))
            {
                MessageBox.Show(Properties.Resources.FRMessage2 + ret);
            }
            else
            {
                MessageBox.Show(Properties.Resources.FRMessage3);
                LabelIsFollowing.Text = Properties.Resources.GetFriendshipInfo1;
                ButtonFollow.Enabled = false;
                ButtonUnFollow.Enabled = true;
            }
        }

        private void ButtonUnFollow_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this._displayUser.ScreenName + Properties.Resources.ButtonUnFollow_ClickText1,
                               Properties.Resources.ButtonUnFollow_ClickText2,
                               MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                string ret = this.Twitter.PostRemoveCommand(this._displayUser.ScreenName);
                if (!string.IsNullOrEmpty(ret))
                {
                    MessageBox.Show(Properties.Resources.FRMessage2 + ret);
                }
                else
                {
                    MessageBox.Show(Properties.Resources.FRMessage3);
                    LabelIsFollowing.Text = Properties.Resources.GetFriendshipInfo2;
                    ButtonFollow.Enabled = true;
                    ButtonUnFollow.Enabled = false;
                }
            }
        }

        private void ShowUserInfo_Activated(object sender, EventArgs e)
        {
            //画面が他画面の裏に隠れると、アイコン画像が再描画されない問題の対応
            if (UserPicture.Image != null)
                UserPicture.Invalidate(false);
        }

        private void ShowUserInfo_Shown(object sender, EventArgs e)
        {
            ButtonClose.Focus();
        }

        private void WebBrowser_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            if (e.Url.AbsoluteUri != "about:blank")
            {
                e.Cancel = true;

                if (e.Url.AbsoluteUri.StartsWith("http://twitter.com/search?q=%23") ||
                    e.Url.AbsoluteUri.StartsWith("https://twitter.com/search?q=%23"))
                {
                    //ハッシュタグの場合は、タブで開く
                    string urlStr = Uri.UnescapeDataString(e.Url.AbsoluteUri);
                    string hash = urlStr.Substring(urlStr.IndexOf("#"));
                    this.Owner.HashSupl.AddItem(hash);
                    this.Owner.HashMgr.AddHashToHistory(hash.Trim(), false);
                    this.Owner.AddNewTabForSearch(hash);
                    return;
                }
                else
                {
                    Match m = Regex.Match(e.Url.AbsoluteUri, @"^https?://twitter.com/(#!/)?(?<ScreenName>[a-zA-Z0-9_]+)$");
                    if (AppendSettingDialog.Instance.OpenUserTimeline && m.Success && this.Owner.IsTwitterId(m.Result("${ScreenName}")))
                    {
                        this.Owner.AddNewTabForUserTimeline(m.Result("${ScreenName}"));
                    }
                    else
                    {
                        this.Owner.OpenUriAsync(e.Url.OriginalString);
                    }
                }
            }
        }

        private void SelectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var browser = (WebBrowser)this.ContextMenuRecentPostBrowser.SourceControl;
            browser.Document.ExecCommand("SelectAll", false, null);
        }

        private void SelectionCopyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var browser = (WebBrowser)this.ContextMenuRecentPostBrowser.SourceControl;
            var selectedText = this.Owner.WebBrowser_GetSelectionText(ref browser);
            if (selectedText != null)
            {
                try
                {
                    Clipboard.SetDataObject(selectedText, false, 5, 100);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void ContextMenuRecentPostBrowser_Opening(object sender, CancelEventArgs e)
        {
            var browser = (WebBrowser)this.ContextMenuRecentPostBrowser.SourceControl;
            var selectedText = this.Owner.WebBrowser_GetSelectionText(ref browser);

            this.SelectionCopyToolStripMenuItem.Enabled = selectedText != null;
        }

        private void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.Owner.OpenUriAsync("https://support.twitter.com/groups/31-twitter-basics/topics/111-features/articles/268350-x8a8d-x8a3c-x6e08-x307f-x30a2-x30ab-x30a6-x30f3-x30c8-x306b-x3064-x3044-x3066");
        }

        private void LinkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            this.Owner.OpenUriAsync("https://support.twitter.com/groups/31-twitter-basics/topics/107-my-profile-account-settings/articles/243055-x516c-x958b-x3001-x975e-x516c-x958b-x30a2-x30ab-x30a6-x30f3-x30c8-x306b-x3064-x3044-x3066");
        }

        private void ButtonSearchPosts_Click(object sender, EventArgs e)
        {
            this.Owner.AddNewTabForUserTimeline(this._displayUser.ScreenName);
        }

        private async void UserPicture_Click(object sender, EventArgs e)
        {
            var imageUrl = this._displayUser.ProfileImageUrlHttps;
            imageUrl = imageUrl.Remove(imageUrl.LastIndexOf("_normal"), 7);

            await this.Owner.OpenUriAsync(imageUrl);
        }

        private bool IsEditing = false;
        private string ButtonEditText = "";

        private async void ButtonEdit_Click(object sender, EventArgs e)
        {
            // 自分以外のプロフィールは変更できない
            if (this.Twitter.UserId != this._displayUser.Id)
                return;

            this.ButtonEdit.Enabled = false;

            if (!IsEditing)
            {
                ButtonEditText = ButtonEdit.Text;
                ButtonEdit.Text = Properties.Resources.UserInfoButtonEdit_ClickText1;

                TextBoxName.Text = LabelName.Text;
                TextBoxName.Enabled = true;
                TextBoxName.Visible = true;
                LabelName.Visible = false;

                TextBoxLocation.Text = LabelLocation.Text;
                TextBoxLocation.Enabled = true;
                TextBoxLocation.Visible = true;
                LabelLocation.Visible = false;

                TextBoxWeb.Text = this._displayUser.Url;
                TextBoxWeb.Enabled = true;
                TextBoxWeb.Visible = true;
                LinkLabelWeb.Visible = false;

                TextBoxDescription.Text = this._displayUser.Description;
                TextBoxDescription.Enabled = true;
                TextBoxDescription.Visible = true;
                DescriptionBrowser.Visible = false;

                TextBoxName.Focus();
                TextBoxName.Select(TextBoxName.Text.Length, 0);

                IsEditing = true;
            }
            else
            {
                if (TextBoxName.Modified ||
                    TextBoxLocation.Modified ||
                    TextBoxWeb.Modified ||
                    TextBoxDescription.Modified)
                {
                    try
                    {
                        var user = await Task.Run(() =>
                            this.Twitter.PostUpdateProfile(
                                this.TextBoxName.Text,
                                this.TextBoxWeb.Text,
                                this.TextBoxLocation.Text,
                                this.TextBoxDescription.Text));

                        this.DisplayUser = user;
                    }
                    catch (WebApiException ex)
                    {
                        MessageBox.Show(ex.Message);
                        return;
                    }
                }

                TextBoxName.Enabled = false;
                TextBoxName.Visible = false;
                LabelName.Visible = true;

                TextBoxLocation.Enabled = false;
                TextBoxLocation.Visible = false;
                LabelLocation.Visible = true;

                TextBoxWeb.Enabled = false;
                TextBoxWeb.Visible = false;
                LinkLabelWeb.Visible = true;

                TextBoxDescription.Enabled = false;
                TextBoxDescription.Visible = false;
                DescriptionBrowser.Visible = true;

                ButtonEdit.Text = ButtonEditText;

                IsEditing = false;
            }

            this.ButtonEdit.Enabled = true;
        }

        private async Task DoChangeIcon(string filename)
        {
            var ret = await Task.Run(() =>
                this.Twitter.PostUpdateProfileImage(filename));

            if (!string.IsNullOrEmpty(ret))
            {
                // "Err:"が付いたエラーメッセージが返ってくる
                MessageBox.Show(ret + Environment.NewLine + Properties.Resources.ChangeIconToolStripMenuItem_ClickText4);
            }
            else
            {
                MessageBox.Show(Properties.Resources.ChangeIconToolStripMenuItem_ClickText5);
            }

            try
            {
                var user = await Task.Run(() =>
                {
                    TwitterDataModel.User result = null;

                    var err = this.Twitter.GetUserInfo(this._displayUser.ScreenName, ref result);
                    if (!string.IsNullOrEmpty(err))
                        throw new WebApiException(err);

                    return result;
                });

                if (user != null)
                    this.DisplayUser = user;
            }
            catch (WebApiException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void ChangeIconToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialogIcon.Filter = Properties.Resources.ChangeIconToolStripMenuItem_ClickText1;
            OpenFileDialogIcon.Title = Properties.Resources.ChangeIconToolStripMenuItem_ClickText2;
            OpenFileDialogIcon.FileName = "";

            DialogResult rslt = OpenFileDialogIcon.ShowDialog();

            if (rslt != DialogResult.OK)
            {
                return;
            }

            string fn = OpenFileDialogIcon.FileName;
            if (this.IsValidIconFile(new FileInfo(fn)))
            {
                await this.DoChangeIcon(fn);
            }
            else
            {
                MessageBox.Show(Properties.Resources.ChangeIconToolStripMenuItem_ClickText6);
            }
        }

        private void ButtonBlock_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this._displayUser.ScreenName + Properties.Resources.ButtonBlock_ClickText1,
                                Properties.Resources.ButtonBlock_ClickText2,
                                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                string res = this.Twitter.PostCreateBlock(this._displayUser.ScreenName);
                if (!string.IsNullOrEmpty(res))
                {
                    MessageBox.Show(res + Environment.NewLine + Properties.Resources.ButtonBlock_ClickText3);
                }
                else
                {
                    MessageBox.Show(Properties.Resources.ButtonBlock_ClickText4);
                }
            }
        }

        private void ButtonReportSpam_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this._displayUser.ScreenName + Properties.Resources.ButtonReportSpam_ClickText1,
                                Properties.Resources.ButtonReportSpam_ClickText2,
                                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                string res = this.Twitter.PostReportSpam(this._displayUser.ScreenName);
                if (!string.IsNullOrEmpty(res))
                {
                    MessageBox.Show(res + Environment.NewLine + Properties.Resources.ButtonReportSpam_ClickText3);
                }
                else
                {
                    MessageBox.Show(Properties.Resources.ButtonReportSpam_ClickText4);
                }
            }
        }

        private void ButtonBlockDestroy_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this._displayUser.ScreenName + Properties.Resources.ButtonBlockDestroy_ClickText1,
                                Properties.Resources.ButtonBlockDestroy_ClickText2,
                                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                string res = this.Twitter.PostDestroyBlock(this._displayUser.ScreenName);
                if (!string.IsNullOrEmpty(res))
                {
                    MessageBox.Show(res + Environment.NewLine + Properties.Resources.ButtonBlockDestroy_ClickText3);
                }
                else
                {
                    MessageBox.Show(Properties.Resources.ButtonBlockDestroy_ClickText4);
                }
            }
        }

        private bool IsValidExtension(string ext)
        {
            ext = ext.ToLower();

            return ext.Equals(".jpg", StringComparison.Ordinal) ||
                ext.Equals(".jpeg", StringComparison.Ordinal) ||
                ext.Equals(".png", StringComparison.Ordinal) ||
                ext.Equals(".gif", StringComparison.Ordinal);
        }

        private bool IsValidIconFile(FileInfo info)
        {
            return this.IsValidExtension(info.Extension) &&
                info.Length < 700 * 1024 &&
                !MyCommon.IsAnimatedGif(info.FullName);
        }

        private void ShowUserInfo_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                if (files.Length != 1)
                    return;

                var finfo = new FileInfo(files[0]);
                if (this.IsValidIconFile(finfo))
                {
                    e.Effect = DragDropEffects.Copy;
                }
            }
        }

        private async void ShowUserInfo_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string filename = ((string[])e.Data.GetData(DataFormats.FileDrop, false))[0];
                await this.DoChangeIcon(filename);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var oldImage = this.UserPicture.Image;
                if (oldImage != null)
                {
                    this.UserPicture.Image = null;
                    oldImage.Dispose();
                }

                if (this.components != null)
                    this.components.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
