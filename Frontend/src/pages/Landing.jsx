import React from 'react'
import { Link } from 'react-router-dom'
import { Button } from '../components/ui/Button'
import { STANDARD_SPECIALTIES } from '../utils/specialtyUtils'
import { DOCTORS } from '../data/doctors'
import { 
  Calendar, 
  Shield, 
  Users, 
  Clock, 
  CheckCircle,
  ArrowRight,
  Phone,
  Mail,
  MapPin,
  DollarSign,
  Briefcase
} from 'lucide-react'

export const Landing = () => {
  return (
    <div className="min-h-screen bg-white">
      {/* Hero Section */}
      <section className="relative bg-gradient-to-br from-primary-dark via-primary-dark to-primary-accent text-white py-20 px-6">
        <div className="max-w-6xl mx-auto">
          <div className="max-w-3xl">
            <h1 className="text-5xl md:text-6xl font-bold mb-6 leading-tight">
              Your Health,<br />Our Priority
            </h1>
            <p className="text-xl md:text-2xl mb-8 text-white/90 leading-relaxed">
              Book appointments with top healthcare professionals across all specialties. Simple, fast, and reliable.
            </p>
            <div className="flex flex-col sm:flex-row gap-4">
              <Link to="/register">
                <Button size="lg" variant="secondary" className="w-full sm:w-auto px-8 py-4 text-base">
                  Get Started
                  <ArrowRight size={20} className="ml-2" />
                </Button>
              </Link>
              <Link to="/login">
                <Button size="lg" variant="outline" className="w-full sm:w-auto px-8 py-4 text-base border-white text-white hover:bg-white/10">
                  Login
                </Button>
              </Link>
            </div>
          </div>
        </div>

        {/* Decorative Elements */}
        <div className="absolute top-20 right-10 w-64 h-64 bg-white/5 rounded-full blur-3xl" />
        <div className="absolute bottom-10 left-10 w-96 h-96 bg-primary-accent/20 rounded-full blur-3xl" />
      </section>

      {/* Specialties Section */}
      <section className="py-20 px-6">
        <div className="max-w-6xl mx-auto">
          <div className="text-center mb-16">
            <h2 className="text-4xl md:text-5xl font-bold text-text-primary mb-4">
              Our Medical Specialties
            </h2>
            <p className="text-xl text-text-secondary max-w-2xl mx-auto">
              Comprehensive healthcare services across all major medical fields
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {STANDARD_SPECIALTIES.map((specialty) => (
              <Link
                key={specialty.id}
                to="/register"
                className="group bg-white border border-gray-200 rounded-2xl p-6 hover:shadow-xl hover:border-primary-accent/50 transition-all duration-300"
              >
                <div className="text-5xl mb-4">{specialty.icon}</div>
                <h3 className="text-xl font-semibold text-text-primary mb-2 group-hover:text-primary-accent transition-colors">
                  {specialty.name}
                </h3>
                <p className="text-text-secondary text-sm leading-relaxed">
                  {specialty.description}
                </p>
                <div className="mt-4 flex items-center text-primary-accent text-sm font-medium opacity-0 group-hover:opacity-100 transition-opacity">
                  Book Now
                  <ArrowRight size={16} className="ml-1" />
                </div>
              </Link>
            ))}
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section className="py-20 px-6 bg-gray-50">
        <div className="max-w-6xl mx-auto">
          <div className="text-center mb-16">
            <h2 className="text-4xl md:text-5xl font-bold text-text-primary mb-4">
              Why Choose Us
            </h2>
            <p className="text-xl text-text-secondary max-w-2xl mx-auto">
              Modern healthcare with convenience at its core
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            <div className="text-center">
              <div className="w-16 h-16 bg-primary-accent/10 rounded-2xl flex items-center justify-center mx-auto mb-6">
                <Calendar size={32} className="text-primary-accent" />
              </div>
              <h3 className="text-xl font-semibold text-text-primary mb-3">
                Easy Booking
              </h3>
              <p className="text-text-secondary leading-relaxed">
                Book appointments online in minutes. No phone calls, no waiting.
              </p>
            </div>

            <div className="text-center">
              <div className="w-16 h-16 bg-primary-accent/10 rounded-2xl flex items-center justify-center mx-auto mb-6">
                <Shield size={32} className="text-primary-accent" />
              </div>
              <h3 className="text-xl font-semibold text-text-primary mb-3">
                Verified Doctors
              </h3>
              <p className="text-text-secondary leading-relaxed">
                All healthcare professionals are verified and highly qualified.
              </p>
            </div>

            <div className="text-center">
              <div className="w-16 h-16 bg-primary-accent/10 rounded-2xl flex items-center justify-center mx-auto mb-6">
                <Clock size={32} className="text-primary-accent" />
              </div>
              <h3 className="text-xl font-semibold text-text-primary mb-3">
                24/7 Support
              </h3>
              <p className="text-text-secondary leading-relaxed">
                Get help anytime with our AI assistant and support team.
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* How It Works Section */}
      <section className="py-20 px-6">
        <div className="max-w-6xl mx-auto">
          <div className="text-center mb-16">
            <h2 className="text-4xl md:text-5xl font-bold text-text-primary mb-4">
              How It Works
            </h2>
            <p className="text-xl text-text-secondary max-w-2xl mx-auto">
              Simple steps to book your appointment
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            <div className="relative">
              <div className="bg-white border border-gray-200 rounded-2xl p-8 relative z-10">
                <div className="w-12 h-12 bg-primary-accent text-white rounded-full flex items-center justify-center text-xl font-bold mb-6">
                  1
                </div>
                <h3 className="text-xl font-semibold text-text-primary mb-3">
                  Choose a Doctor
                </h3>
                <p className="text-text-secondary leading-relaxed">
                  Browse our verified healthcare professionals by specialty
                </p>
              </div>
              <div className="hidden md:block absolute top-1/2 -right-4 w-8 h-0.5 bg-gray-200 transform -translate-y-1/2" />
            </div>

            <div className="relative">
              <div className="bg-white border border-gray-200 rounded-2xl p-8 relative z-10">
                <div className="w-12 h-12 bg-primary-accent text-white rounded-full flex items-center justify-center text-xl font-bold mb-6">
                  2
                </div>
                <h3 className="text-xl font-semibold text-text-primary mb-3">
                  Book Appointment
                </h3>
                <p className="text-text-secondary leading-relaxed">
                  Select your preferred date and time slot
                </p>
              </div>
              <div className="hidden md:block absolute top-1/2 -right-4 w-8 h-0.5 bg-gray-200 transform -translate-y-1/2" />
            </div>

            <div>
              <div className="bg-white border border-gray-200 rounded-2xl p-8">
                <div className="w-12 h-12 bg-primary-accent text-white rounded-full flex items-center justify-center text-xl font-bold mb-6">
                  3
                </div>
                <h3 className="text-xl font-semibold text-text-primary mb-3">
                  Get Confirmation
                </h3>
                <p className="text-text-secondary leading-relaxed">
                  Receive instant confirmation and appointment details
                </p>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Featured Doctors Section */}
      <section className="py-20 px-6">
        <div className="max-w-6xl mx-auto">
          <div className="text-center mb-16">
            <h2 className="text-4xl md:text-5xl font-bold text-text-primary mb-4">
              Meet Our Top Doctors
            </h2>
            <p className="text-xl text-text-secondary max-w-2xl mx-auto">
              Highly qualified healthcare professionals ready to help you
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
            {DOCTORS.slice(0, 8).map((doctor) => (
              <Link
                key={doctor.id}
                to="/register"
                className="group bg-white border border-gray-200 rounded-2xl overflow-hidden hover:shadow-xl hover:border-primary-accent/50 transition-all duration-300"
              >
                <div className="h-64 bg-gradient-to-br from-primary-accent/20 to-primary-dark/10 relative overflow-hidden">
                  <img
                    src={doctor.image}
                    alt={doctor.name}
                    className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
                  />
                  <div className="absolute bottom-0 left-0 right-0 bg-gradient-to-t from-black/60 to-transparent p-4">
                    <span className="text-white text-sm font-medium bg-primary-accent/90 px-3 py-1 rounded-full">
                      {doctor.initials}
                    </span>
                  </div>
                </div>
                <div className="p-5">
                  <h3 className="font-semibold text-text-primary mb-1">{doctor.name}</h3>
                  <p className="text-primary-accent text-sm font-medium mb-2">{doctor.specialty}</p>
                  <p className="text-text-secondary text-sm line-clamp-2 mb-3">
                    {doctor.description}
                  </p>
                  <div className="flex items-center gap-4 text-sm text-text-muted mb-3">
                    <div className="flex items-center gap-1">
                      <Briefcase size={14} />
                      <span>{doctor.experience} years</span>
                    </div>
                    <div className="flex items-center gap-1">
                      <DollarSign size={14} />
                      <span>${doctor.price}</span>
                    </div>
                  </div>
                  <div className="flex items-center justify-between">
                    <div className="flex items-center text-yellow-500">
                      <span>{'★'.repeat(Math.floor(doctor.rating))}</span>
                      <span className="text-text-muted text-sm ml-2">{doctor.rating}</span>
                    </div>
                    <span className="text-primary-accent text-sm font-medium opacity-0 group-hover:opacity-100 transition-opacity">
                      Book Now
                    </span>
                  </div>
                </div>
              </Link>
            ))}
          </div>

          <div className="text-center mt-10">
            <Link to="/register">
              <Button size="lg" variant="primary" className="px-8 py-3">
                View All Doctors
                <ArrowRight size={18} className="ml-2" />
              </Button>
            </Link>
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="py-20 px-6 bg-primary-dark text-white">
        <div className="max-w-4xl mx-auto text-center">
          <h2 className="text-4xl md:text-5xl font-bold mb-6">
            Ready to Book Your Appointment?
          </h2>
          <p className="text-xl text-white/90 mb-8 max-w-2xl mx-auto">
            Join thousands of satisfied patients who trust us with their healthcare
          </p>
          <Link to="/register">
            <Button size="lg" variant="secondary" className="px-10 py-4 text-base">
              Create Free Account
              <ArrowRight size={20} className="ml-2" />
            </Button>
          </Link>
        </div>
      </section>

      {/* Footer */}
      <footer className="bg-gray-900 text-white py-12 px-6">
        <div className="max-w-6xl mx-auto">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-8 mb-8">
            <div>
              <h3 className="text-xl font-bold mb-4">Contact Us</h3>
              <div className="space-y-3">
                <div className="flex items-center gap-3 text-gray-300">
                  <Phone size={18} />
                  <span>+373 22 123 456</span>
                </div>
                <div className="flex items-center gap-3 text-gray-300">
                  <Mail size={18} />
                  <span>info@appointment-app.md</span>
                </div>
                <div className="flex items-center gap-3 text-gray-300">
                  <MapPin size={18} />
                  <span>Chișinău, Moldova</span>
                </div>
              </div>
            </div>

            <div>
              <h3 className="text-xl font-bold mb-4">Quick Links</h3>
              <div className="space-y-2">
                <Link to="/register" className="block text-gray-300 hover:text-white transition-colors">
                  Register
                </Link>
                <Link to="/login" className="block text-gray-300 hover:text-white transition-colors">
                  Login
                </Link>
                <Link to="/register" className="block text-gray-300 hover:text-white transition-colors">
                  Find Doctors
                </Link>
              </div>
            </div>

            <div>
              <h3 className="text-xl font-bold mb-4">Working Hours</h3>
              <div className="space-y-2 text-gray-300">
                <p>Monday - Friday: 8:00 - 19:00</p>
                <p>Saturday: 8:00 - 13:00</p>
                <p>Sunday: Closed</p>
              </div>
            </div>
          </div>

          <div className="border-t border-gray-800 pt-8 text-center text-gray-400">
            <p>&copy; 2026 Appointment App. All rights reserved.</p>
          </div>
        </div>
      </footer>
    </div>
  )
}