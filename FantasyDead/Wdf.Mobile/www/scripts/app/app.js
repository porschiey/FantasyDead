﻿(function () {

    var defaultUser = { username: 'guest', loggedIn: false, id: '', key: '', isNewUser: false }; //default

    var loginOptions = { redirect_uri: 'https://thefantasydead.com/callback' };

    var app = angular.module('wdf', ['ngCordova', 'ngCordovaOauth', 'wdf.home', 'wdf.roster', 'wdf.settings', 'wdf.leaderboard', 'wdf.stats', 'wdf.events', 'chart.js']);

    app.directive('fileModel', ['$parse', function ($parse) {
        return {
            restrict: 'A',
            link: function (scope, element, attrs) {
                var model = $parse(attrs.fileModel);
                var modelSetter = model.assign;

                element.bind('change', function () {
                    scope.$apply(function () {
                        modelSetter(scope, element[0].files[0]);
                    });
                });
            }
        };
    }]);

    app.run(['$rootScope', '$cordovaOauth', '$location', '$http', function ($rootScope, $cordovaOauth, $location, $http) {
        document.addEventListener("deviceready", function () {

            //$rootScope.fdApi = 'http://192.168.1.2/';
            $rootScope.fdApi = 'http://thefantasydead.com/';
            $rootScope.loading = true;
            var init = function () {

                //discover login state.                
                var uString = localStorage.getItem('user');
                $rootScope.user = defaultUser; //default


                if (!uString) { //not logged in
                    $location.path('/home');
                } else {
                    try {
                        $rootScope.user = JSON.parse(uString);
                        $rootScope.user.isNewUser = $rootScope.user.Role === 1;
                        if ($rootScope.user.isNewUser)
                            $location.path('/settings');
                        else {
                            console.log('user is already logged in');
                            $rootScope.authorize($rootScope.user.Token);
                        }

                    } catch (e) {
                        console.error(JSON.stringify(e));
                        //reset and send home
                        localStorage.clear();
                        $location.path('/home');
                    }

                    if ($rootScope.user.Configuration.ReceiveNotifications)
                        $rootScope.setupPushNotification();
                }

                $rootScope.deviceReady = true;
                $rootScope.loading = false;

                var currentPlat = device.platform.toLowerCase();

                //setup push here


                //house keeping
                if (currentPlat === 'android') {
                    window.plugins.headerColor.tint('#000000');
                }

                if (!$rootScope.$$phase) $rootScope.$digest();
            };


            $rootScope.destroyPushSetup = function () {
                $http.get($rootScope.fdApi + 'api/person/push/cancel').then(function (response) {
                    $rootScope.saveUserChanges();
                }).catch($rootScope.handleError);
            };

            var pushOpts = {
                android: {
                    senderID: '20690878081'
                }
            };

            $rootScope.setupPushNotification = function () {

                console.log('setting up push');
                PushNotification.hasPermission(function (perm) {
                    if (perm.isEnabled) {
                        var pN = PushNotification.init(pushOpts);
                        console.log('push enabled');
                        pN.on('registration', function (data) {

                            console.log('push reg: ' + JSON.stringify(data));
                            var pushReq = { device: 'android', registrationId: data.registrationId };
                            $http.put($rootScope.fdApi + 'api/person/push/register', pushReq).then(function (response) {
                                $rootScope.user.PushRegistration = data.registrationId;
                                $rootScope.user.ReceiveNotifications = true;
                                $rootScope.saveUserChanges();
                            }).catch($rootScope.handleError);
                        });
                        pN.on('error', function (e) {
                            console.error(e.message);
                        });

                        pN.on('notification', function (data) {
                            console.log(data);

                            $rootScope.notificationMsg = data.message;
                            if (!$scope.$digest) $scope.$apply();

                            $('#pushModal').modal();
                        });
                    }
                });
            };

            $rootScope.die = function () {
                $('#betaModal').modal('hide');
            };

            $rootScope.betaExit = new Date();
            $rootScope.betaLoop = function () {
                if (new Date().getTime() < $rootScope.betaExit.getTime()) {

                    var ms = $rootScope.betaExit.getTime() - new Date().getTime();
                    var secs = Math.ceil(ms / 1000);
                    $('#time-left').text(parseInt(secs));
                    setTimeout($rootScope.betaLoop, 1000);
                } else {
                    $('#betaModal').modal('hide');
                }
            };

            $rootScope.showBetaDisclaimer = function () {
                $('#betaModal').modal();
                $rootScope.betaExit = new Date(new Date().getTime() + 10000);
                $rootScope.betaLoop();
            };

            //uses login data to authorize with fantasy dead api
            $rootScope.authorize = function (token, noRD) {
                if (noRD === undefined) {
                    noRD = false;
                }

                if (token === undefined && $rootScope.user.loggedIn)
                    token = $rootScope.user.Token;

                $http.defaults.headers.common.Authorization = 'Bearer ' + token;
                console.log('fetching profile...');
                $http.get($rootScope.fdApi + 'api/person').then(function (response) {


                    var u = response.data;
                    $rootScope.user = u;
                    $rootScope.user.Token = token;
                    $rootScope.user.loggedIn = true;
                    $rootScope.user.isNewUser = u.Role === 1;
                    console.log('user...: ' + JSON.stringify($rootScope.user));

                    $rootScope.saveUserChanges();

                    if ($rootScope.user.isNewUser) {
                        $location.path('/settings');
                        $rootScope.user.askNotify = true;
                    }
                    else {
                        if (!noRD)
                            $location.path('/roster');
                    }

                    if (!$rootScope.$$phase)
                        $rootScope.$digest();

                }).catch(function (error) {
                    $rootScope.loggingIn = false;
                    $rootScope.handleError(error);
                    $rootScope.showError('There was an error while logging you in. Please try again later.');
                });


            };

            init();
        });
    }]);

    app.config(['$routeProvider', '$locationProvider', '$httpProvider', 'ChartJsProvider', function ($routeProvider, $locationProvider, $httpProvider, ChartJsProvider) {



        $routeProvider
            .when('/home',
            {
                templateUrl: 'scripts/app/home.html',
                controller: 'homeController'
            })
        .when('/roster/:personId?',
        {
            templateUrl: 'scripts/app/roster.html',
            controller: 'rosterController'
        })
              .when('/leaderboard',
        {
            templateUrl: 'scripts/app/leaderboard.html',
            controller: 'leaderboardController'
        })
        .when('/settings/',
        {
            templateUrl: 'scripts/app/settings.html',
            controller: 'settingsController'
        })
            .when('/events',
        {
            templateUrl: 'scripts/app/events.html',
            controller: 'eventsController'
        })
            .when('/stats',
        {
            templateUrl: 'scripts/app/stats.html',
            controller: 'statsController'
        });


        $locationProvider.html5Mode(false);

    }]);

    //master controller for nav and global elements/content
    app.controller('masterController', ['$scope', '$rootScope', '$cordovaOauth', '$location', '$http', function ($scope, $rootScope, $cordovaOauth, $location, $http) {


        document.addEventListener("deviceready", function () {

            //backbutton
            document.addEventListener("backbutton", function (e) {

                var closedModal = false;
                $('.modal').each(function () {
                    if ($(this).hasClass('in')) {

                        if ($(this).attr('id') === 'charModal') {
                            $rootScope.$emit('flipSlot');
                        } else {
                            $(this).modal('hide');
                            closedModal = true;
                        }
                    }
                });

                if (closedModal)
                    e.preventDefault();

            });

            $rootScope.handleError = function (error) {
                if (typeof error === 'string')
                    console.error(error);
                else
                    console.error(JSON.stringify(error));

                if (error.status) {
                    if (error.status === 401) {
                        //token expired, log out
                        $rootScope.logout();
                        return;
                    }
                }
            };

            $rootScope.showError = function (error) {

                if (typeof error === typeof '')
                    $rootScope.errorMessage = error;

                if (typeof error === typeof {}) {

                    if (typeof error.data == typeof {} && error.data.Message) {

                        $rootScope.errorMessage = error.data.Message;
                        $('#errorModal').modal();
                        return;
                    }

                    if (error.status) {
                        $rootScope.errorMessage = error.status + ': ' + error.statusText;
                    }
                    else {
                        $rootScope.errorMessage = JSON.stringify(error);
                    }
                }

                $('#errorModal').modal();
            };

            var round = function (value, decimals) {
                return Number(Math.round(value + 'e' + decimals) + 'e-' + decimals);
            };

            $rootScope.generatePointValue = function (raw) {

                var str = '' + parseFloat(raw);
                var parts = str.split('.');
                var whole = parts[0];

                var dec = parts.length === 1 ? '00' : '' + round(parseInt(parts[1]), 2);
                return { whole: whole, dec: dec };
            };

            $rootScope.isAFriend = function (id) {
                try {

                    var isFriend = false;
                    $.each($rootScope.user.Friends, function (ix, f) {

                        if (f === id) {
                            isFriend = true;
                            return false;
                        }
                    });

                    return isFriend;
                } catch (e) {
                    return false;
                }
            };


            //toggles.. the menu!
            $rootScope.menuOpen = false;
            $rootScope.toggleMenu = function () {
                $rootScope.menuOpen = !$rootScope.menuOpen;
            };

            $('body').on('click', 'div.menu a.list-group-item', function () {
                $rootScope.menuOpen = false;
                if (!$scope.$$phase) $scope.$digest();
            });

            //watches route changes.
            $rootScope.$on('$routeChangeStart', function (e, next, current) {
                $('.modal').modal('hide'); //house keeping.             

                if (next.$$route.originalPath !== '/home' && !$rootScope.user.loggedIn) {
                    $location.path('/home');
                }
            });

            $rootScope.$on('$routeChangeSuccess', function (e, current, previous) {
                $rootScope.path = $location.path().replace(/\//ig, '');
            });

            $rootScope.logout = function () {
                $rootScope.loggingIn = false;
                //TODO: unregister push
                $http.defaults.headers.common.Authorization = '';

                localStorage.clear();
                $rootScope.user = defaultUser;
                $location.path('/home');
            };




            //// LOGINS

            //starts a facebook login flow
            $rootScope.facebookLogin = function () {

                $('#loginModal').modal('hide');
                $rootScope.loggingIn = true;
                $cordovaOauth.facebook('511139795716212', ['email', 'public_profile'], loginOptions).then(function (result) {

                    var lr = { token: result.access_token, platform: 1 };
                    $http.put($rootScope.fdApi + 'api/register/social', lr).then(function (response) {
                        $rootScope.authorize(response.data);
                    }).catch(function (error) {
                        $rootScope.handleError(error);
                        $rootScope.showError('Could not log you into Facebook: Login servers had an issue. Please try again later.');
                    });
                })
            };

            //start a google login flow
            $rootScope.googleLogin = function () {
                $('#loginModal').modal('hide');
                $rootScope.loggingIn = true;
                $cordovaOauth.google('231469866429-ci03k4pvepithnhft6ols5oq896um4fi.apps.googleusercontent.com', ['email', 'profile'],
                    loginOptions).then(function (result) {


                        var lr = { token: result.access_token, platform: 2 };
                        $http.put($rootScope.fdApi + 'api/register/social', lr).then(function (response) {
                            $rootScope.authorize(response.data);
                        }).catch(function (error) {
                            $rootScope.handleError(error);
                            $rootScope.showError('Could not log you into Google: Login servers had an issue. Please try again later.');
                        });

                        //$http.get('https://www.googleapis.com/oauth2/v1/userinfo?alt=json&access_token=' + result.access_token)
                        //    .then(function (response) {
                        //        authorize('google', response.data);
                        //    }).catch(function (error) {
                        //        console.error(error);
                        //    });
                    }, function (error) {
                        console.error(error);
                    });
            };

            //starts a twitter login flow
            $rootScope.twitterLogin = function () {
                $('#loginModal').modal('hide');
                $rootScope.loggingIn = true;
                $cordovaOauth.twitter('EBJIutCaB6XiNvbGe6oexBYKf', '16XiJIz8j9KKFFNYeAkt5EZEMQqE5Rtc3tPBbFadnxR8VaGWzR', loginOptions).then(function (data) {
                    try {

                        var lr = { token: data.oauth_token + ',' + data.oauth_token_secret, platform: 0 };
                        $http.put($rootScope.fdApi + 'api/register/social', lr).then(function (response) {
                            $rootScope.authorize(response.data);
                        }).catch(function (error) {
                            $rootScope.handleError(error);
                            $rootScope.loggingIn = false;
                            $rootScope.showError('Could not log you into Twitter: Login servers had an issue. Please try again later.');
                        });

                    } catch (e) {
                        console.error(JSON.stringify(e));
                    }

                }, function (error) {
                    console.error(error);
                });
            };

            //starts a microsoft login flow
            $rootScope.microsoftLogin = function () {
                $('#loginModal').modal('hide');
                $rootScope.loggingIn = true;
                $cordovaOauth.windowsLive('0000000040167932', ['wl.basic', 'wl.emails'], loginOptions).then(function (result) {


                    var lr = { token: result.access_token, platform: 3 };
                    $http.put($rootScope.fdApi + 'api/register/social', lr).then(function (response) {
                        $rootScope.authorize(response.data);
                    }).catch(function (error) {
                        $rootScope.handleError(error);
                        $rootScope.showError('Could not log you into Microsoft: Login servers had an issue. Please try again later.');
                    });
                }, function (error) {
                    console.error(error);
                });
            };

            //updates the user in storage and in the api.
            $rootScope.saveUserChanges = function (updateApi) {
                if (!$rootScope.user) {
                    console.error('user invalid, cannot save.');
                    return;
                }

                //update api
                if (updateApi === undefined)
                    updateApi = false;
                if (updateApi) {
                    //api returns truer object, inject that
                    //$rootScope.user = ...
                }

                $rootScope.user.ReceiveNotifications = ($rootScope.user.PushRegistration !== null);

                localStorage.setItem('user', JSON.stringify($rootScope.user));
            };




            //toggle the configuration for notifications on score completion
            $rootScope.toggleNotifyWhenScored = function () {
                $rootScope.user.Configuration.NotifyWhenScored = !$rootScope.user.Configuration.NotifyWhenScored;
                $rootScope.updateConfiguration('NotifyWhenScored', $rootScope.user.Configuration.NotifyWhenScored);
            };

            //toggling notifications all up
            $rootScope.toggleNotifications = function (force) {

                $rootScope.user.Configuration.ReceiveNotifications = !$rootScope.user.Configuration.ReceiveNotifications;

                if (typeof force === typeof true)
                    $rootScope.user.Configuration.ReceiveNotifications = force;

                if ($rootScope.user.Configuration.ReceiveNotifications) {
                    $rootScope.setupPushNotification();
                }
                else {
                    $rootScope.destroyPushSetup();
                }

            };


            //navigate to the roster page and look up a profile
            $rootScope.view = function (personId) {
                $location.path('/roster/' + personId);
            };


            $rootScope.generateDeadlineHoursOption = function () {

                if ($rootScope.user.Configuration.DeadlineReminderHours === 0)
                    return { hours: 0, d: 'Don\'t notify me' };

                return { hours: $rootScope.user.Configuration.DeadlineReminderHours, d: $rootScope.user.Configuration.DeadlineReminderHours + ' hour(s) before' };;
            };
            $rootScope.setDeadlineHours = function (v) {
                $rootScope.user.Configuration.DeadlineReminderHours = v;
                $rootScope.updateConfiguration('DeadlineReminderHours', v);
            };

            //fires configuration change to api
            $rootScope.updateConfiguration = function (key, value) {
                $http.post($rootScope.fdApi + 'api/person/config/' + key, value).then(function (response) {
                    $rootScope.saveUserChanges();
                }).catch(function (error) {
                    $rootScope.handleError(error);
                    $rootScope.showError(error);
                });
            };

            //minor object setup
            $rootScope.preLockHours = [
                  { hours: 0, d: 'Don\'t notify me' },
                { hours: 1, d: '1 Hour Before' },
                { hours: 2, d: '2 Hours Before' },
                { hours: 4, d: '4 Hours Before' },
                { hours: 6, d: '6 Hours Before' },
                { hours: 12, d: '12 Hours Before' },
                { hours: 24, d: '24 Hours Before' },
                { hours: 48, d: '48 Hours Before' },
                { hours: 72, d: '72 Hours Before' }
            ];
        });
    }]);
})();


//document.addEventListener("backbutton", onBackKeyDown, false);
//function onBackKeyDown(e) {
//    e.preventDefault();
//}

